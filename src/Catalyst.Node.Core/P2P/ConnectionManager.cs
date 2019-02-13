using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Core.Events;
using Catalyst.Node.Core.Helpers.IO;
using Catalyst.Node.Core.Helpers.Network;
using Catalyst.Node.Core.Helpers.Streams;
using Catalyst.Node.Core.Helpers.Util;
using Catalyst.Node.Core.Listeners;
using Catalyst.Node.Core.Messages;
using Catalyst.Node.Core.Modules.P2P.Messages;
using Catalyst.Protocol.IPPN;
using Dawn;
using Org.BouncyCastle.Security;
using Serilog;
using Serilog.Core;

namespace Catalyst.Node.Core.P2P
{
    /// <summary>
    /// </summary>
    public class ConnectionManager : IDisposable
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ILogger _logger;
        private readonly MessageReplyWaitManager _messageReplyManager;
        private int _activeConnections;

        /// <summary>
        /// </summary>
        /// <param name="sslCertificate"></param>
        /// <param name="peerList"></param>
        /// <param name="messageQueueManager"></param>
        /// <param name="nodeIdentity"></param>
        public ConnectionManager(X509Certificate2 sslCertificate,
            PeerList peerList,
            MessageQueueManager messageQueueManager,
            PeerIdentifier nodeIdentity,
            ILogger logger)
        {
            _logger = logger;
            PeerList = peerList;
            _activeConnections = 0;
            AcceptInvalidCerts = true;
            NodeIdentity = nodeIdentity;
            SslCertificate = sslCertificate;
            MessageQueueManager = messageQueueManager;
            PeerList.OnAddedUnIdentifiedConnection += AddedConnectionHandler;
            _messageReplyManager = new MessageReplyWaitManager(MessageQueueManager, PeerList);
        }

        private bool Disposed { get; set; }
        internal PeerList PeerList { get; }
        private TcpListener Listener { get; set; }
        private CancellationToken Token { get; set; }
        private bool AcceptInvalidCerts { get; }
        private PeerIdentifier NodeIdentity { get; }
        private X509Certificate2 SslCertificate { get; }
        private MessageQueueManager MessageQueueManager { get; }
        private CancellationTokenSource CancellationToken { get; set; }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            _logger.Verbose("disposing network class");
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// </summary>
        public event EventHandler<AnnounceNodeEventArgs> AnnounceNode;

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void AddedConnectionHandler(object sender, NewUnIdentifiedConnectionEventArgs eventArgs)
        {
            _logger.Information("Starting Challenge Request");

            var challengeRequest = new PeerProtocol.Types.ChallengeRequest();
            var random = new SecureRandom();
            var keyBytes = new byte[16];
            random.NextBytes(keyBytes);
            challengeRequest.Nonce = random.NextInt();

            var requestMessage = MessageFactory.RequestFactory(1, 3, eventArgs.Connection, challengeRequest);

            _messageReplyManager.Add(requestMessage);
            _logger.Information("trace msg handler");
        }

        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private async Task DataReceiver(Connection connection, CancellationToken cancelToken)
        {
            Guard.Argument(connection, nameof(connection)).NotNull();

            var streamReadCounter = 0;

            try
            {
                while (!Token.IsCancellationRequested)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    if (!connection.IsConnected())
                    {
                        _logger.Warning("*** Data receiver can not attach to connection");
                        break;
                    }

                    var payload = Reader.MessageRead(connection.SslStream);

                    if (payload == null)
                    {
                        await Task.Delay(30, Token);
                        streamReadCounter += streamReadCounter;
                        // count how many times we try reading header && content so we don't get stuck in here.
                        if (streamReadCounter == 5) break;
                    }
                    else
                    {
                        // we need to learn the message type here
                        var msgDescriptor = payload.Slice(0, 2);
                        var messageBytes = payload.Slice(2);
                        var message = MessageFactory.ResponseFactory(msgDescriptor[0], msgDescriptor[1], connection,
                            messageBytes);
                        lock (MessageQueueManager._receivedMessageQueue)
                        {
                            MessageQueueManager._receivedMessageQueue.Enqueue(message);
                            _logger.Debug("messages in queue: " + MessageQueueManager._receivedMessageQueue.Count);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to receive data for connection {0}:{1}",
                    connection.EndPoint.Address, connection.EndPoint.Port);
                throw;
            }
            finally
            {
                await Task.Run(() => DisconnectConnection(connection), Token);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="ipEndPoint"></param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="Exception"></exception>
        internal async Task InboundConnectionListener(IPEndPoint ipEndPoint)
        {
            //@TODO put in try catch
            Listener = ListenerFactory.CreateTcpListener(ipEndPoint);

            //@TODO put in try catch
            Listener.Start();
            _logger.Information("Peer server starting on " + ipEndPoint.Address + ":" + ipEndPoint.Port);

            try
            {
                _logger.Debug($"Raising {nameof(AnnounceNodeEventArgs)} for node {NodeIdentity}");
                await Events.Events.AsyncRaiseEvent(AnnounceNode, this, new AnnounceNodeEventArgs(NodeIdentity));
            }
            catch (ArgumentNullException e)
            {
                _logger.Error(e, "Events.Raise(AnnounceNodeEventArgs)");
            }

            while (!Token.IsCancellationRequested)
                try
                {
                    var tcpClient = await Listener.AcceptTcpClientAsync();
                    tcpClient.LingerState.Enabled = false;

                    if (PeerList.CheckIfIpBanned(tcpClient))
                    {
                        // incoming endpoint is in banned list so peace out bro! ☮ ☮ ☮ ☮ 
                        tcpClient.Dispose();
                        continue;
                    }

                    var connection = StartPeerConnection(tcpClient);
                    if (connection == null) continue;
                    
                    using (connection)
                    {
                        try
                        {
                            connection = GetPeerConnectionTlsStream(connection, 1);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "Failed to get peer connection for {0}", connection.EndPoint);
                            DisconnectConnection(connection);
                            continue;
                        }

                        await DataReceiver(connection, Token);
                    }
                }
                catch (SocketException ex)
                {
                    switch (ex.Message)
                    {
                        case "An existing connection was forcibly closed by the remote host":
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("*** AcceptConnections Exception from ", ex);
                }
        }

        /// <summary>
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        public async void BuildOutBoundConnection(string ip, int port)
        {
            if (string.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
            if (!Ip.ValidPortRange(port)) throw new ArgumentOutOfRangeException(nameof(port));

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    var targetEndpoint = EndpointBuilder.BuildNewEndPoint(ip, port);
                    
                    IAsyncResult asyncClient;
                    var timeoutCancelationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    tcpClient.ConnectAsync(ip, port).Wait(timeoutCancelationSource.Token);
                    var connection = StartPeerConnection(tcpClient);

                    await DataReceiver(connection, Token);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to build connection to {0}:{1}", ip, port);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private Connection StartPeerConnection(TcpClient tcpClient)
        {
            if (tcpClient == null) throw new ArgumentNullException(nameof(tcpClient));

            var connection = new Connection(tcpClient);
            var activeCount = Interlocked.Increment(ref _activeConnections);
            Log.Information("*** Connection to {0}:{1} created. {2} connections in total.", 
                connection.EndPoint.Address, connection.EndPoint.Port, activeCount);
            return connection;
        }

        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="direction"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        private Connection GetPeerConnectionTlsStream(Connection connection, int direction, IPEndPoint endPoint = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            connection.SslStream = TlsStream.GetTlsStream(
                connection.TcpClient.GetStream(),
                direction,
                SslCertificate,
                AcceptInvalidCerts,
                false,
                endPoint
            );

            if (connection.SslStream == null || connection.SslStream.GetType() != typeof(SslStream))
                throw new ArgumentNullException(nameof(SslStream));

            if (!PeerList.AddUnidentifiedConnectionToList(connection))
            {
                connection.Dispose();
                throw new InvalidOperationException("unable to add connection to unidentified list");
            }

            return connection;
        }

        /// <summary>
        ///     Disconnects a connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        private bool DisconnectConnection(Connection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            
            try
            {
                // first check our unidentified connections
                if (!PeerList.RemoveUnidentifiedConnectionFromList(connection))
                {
                    // its not in our unidentified list so now check the peer bucket
                    if (!PeerList.FindPeerFromConnection(connection, out var peer)) return false;
                    if (PeerList.RemovePeerFromBucket(peer))
                    {
                        peer.Dispose();
                    }
                    else
                    {
                        connection.Dispose();
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to disconnect peer at {0}", connection?.EndPoint?.ToString() ?? "unknown");
                return false;
            }
            finally
            {
                var activeCount = Interlocked.Decrement(ref _activeConnections);
                Log.Information("***** Connection successfully disconnected connected (now " + activeCount +
                            " connections active)");
            }
        }

        /// <summary>
        ///     dispose server and background workers.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                CancellationToken.Cancel();
                CancellationToken.Dispose();

                if (Listener?.Server != null)
                {
                    Listener.Server.Close();
                    Listener.Server.Dispose();
                }

                if (PeerList.UnIdentifiedPeers?.Count > 0)
                    foreach (var peer in PeerList.UnIdentifiedPeers)
                        peer.Value.Dispose();

                if (PeerList.PeerBucket?.Count > 0)
                    foreach (var peer in PeerList.PeerBucket)
                        peer.Value.Dispose();
            }

            Disposed = true;
            Logger.Verbose("Catalyst.Helpers.Network class disposed");
        }
    }
}