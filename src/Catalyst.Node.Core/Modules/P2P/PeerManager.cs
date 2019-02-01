using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Core.Helpers.IO;
using Catalyst.Node.Core.Helpers.Logger;
using Catalyst.Node.Core.Helpers.Network;
using Catalyst.Node.Core.Helpers.Streams;
using Catalyst.Node.Core.Helpers.Util;
using Catalyst.Node.Core.Modules.P2P.Events;
using Catalyst.Node.Core.Modules.P2P.Listeners;
using Catalyst.Node.Core.Modules.P2P.Messages;
using Catalyst.Protocol.Peer;
using Dawn;
using Org.BouncyCastle.Security;

namespace Catalyst.Node.Core.Modules.P2P
{
    /// <summary>
    /// </summary>
    public class PeerManager : IDisposable
    {
        private readonly MessageReplyWaitManager MessageReplyManager;
        private int ActiveConnections;

        /// <summary>
        /// </summary>
        /// <param name="sslCertificate"></param>
        /// <param name="peerList"></param>
        /// <param name="messageQueueManager"></param>
        /// <param name="nodeIdentity"></param>
        public PeerManager(X509Certificate2 sslCertificate, PeerList peerList, MessageQueueManager messageQueueManager,
            PeerIdentifier nodeIdentity)
        {
            PeerList = peerList;
            ActiveConnections = 0;
            AcceptInvalidCerts = true;
            NodeIdentity = nodeIdentity;
            SslCertificate = sslCertificate;
            MessageQueueManager = messageQueueManager;
            PeerList.OnAddedUnIdentifiedConnection += AddedConnectionHandler;
            MessageReplyManager = new MessageReplyWaitManager(MessageQueueManager, PeerList);
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
            Log.Message("disposing network class");
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// </summary>
//        public event EventHandler<AnnounceNodeEventArgs> AnnounceNode;

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void AddedConnectionHandler(object sender, NewUnIdentifiedConnectionEventArgs eventArgs)
        {
            Log.Message("Starting Challenge Request");

            var challengeRequest = new PeerProtocol.Types.ChallengeRequest();
            var random = new SecureRandom();
            var keyBytes = new byte[16];
            random.NextBytes(keyBytes);
            challengeRequest.Nonce = random.NextInt();

            var requestMessage = MessageFactory.RequestFactory(1, 3, eventArgs.Connection, challengeRequest);

            MessageReplyManager.Add(requestMessage);
            Log.Message("trace msg handler");
        }

        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private async Task<bool> DataReceiver(Connection connection, CancellationToken cancelToken)
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
                        Log.Message("*** Data receiver can not attach to connection");
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
                            Log.Message("messages in queue: " + MessageQueueManager._receivedMessageQueue.Count);
                        }
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                LogException.Message(
                    "*** Data receiver cancelled " + connection.EndPoint.Address + ":" + connection.EndPoint.Port +
                    " disconnected", e);
                throw;
            }
            catch (Exception e)
            {
                LogException.Message(
                    "*** Data receiver exception " + connection.EndPoint.Address + ":" + connection.EndPoint.Port +
                    " disconnected", e);
                throw;
            }
            finally
            {
                await Task.Run(() => DisconnectConnection(connection), Token);
            }

            return true;
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
            Log.Message("Peer server starting on " + ipEndPoint.Address + ":" + ipEndPoint.Port);

//            try
//            {
//                await AsyncRaiseEvent(AnnounceNode, this, new AnnounceNodeEventArgs(NodeIdentity));
//            }
//            catch (ArgumentNullException e)
//            {
//                LogException.Message("InboundConnectionListener: Events.Raise(AnnounceNodeEventArgs)", e);
//            }

            while (!Token.IsCancellationRequested)
                try
                {
                    var tcpClient = await Listener.AcceptTcpClientAsync();
                    tcpClient.LingerState.Enabled = false;

                    try
                    {
                        if (PeerList.CheckIfIpBanned(tcpClient))
                        {
                            // incoming endpoint is in banned list so peace out bro! ☮ ☮ ☮ ☮ 
                            tcpClient.Dispose();
                            continue;
                        }
                    }
                    catch (ArgumentNullException e)
                    {
                        tcpClient.Dispose();
                        LogException.Message("InboundConnectionListener: CheckIfIpBanned", e);
                        continue;
                    }
                    
                    Connection connection;
                    try
                    {
                        connection = StartPeerConnection(tcpClient);
                        if (connection == null) continue;
                    }
                    catch (Exception e)
                    {
                        LogException.Message("InboundConnectionListener: StartPeerConnection", e);
                        continue;
                    }

                    using (connection)
                    {
                        try
                        {
                            connection = GetPeerConnectionTlsStream(connection, 1);
                        }
                        catch (Exception e)
                        {
                            DisconnectConnection(connection);
                            LogException.Message("InboundConnectionListener: GetPeerConnectionTlsStream", e);
                            continue;
                        }

                        await DataReceiver(connection, Token);
//                        Task.Run(async () => await DataReceiver(connection, Token), Token);

//                        Log.Message("*** FinalizeConnection unable to add peer " + connection.EndPoint.Address + connection.EndPoint.Port);
//                        throw new Exception("unable to add connection as peer");                        
                    }
                }
                catch (AuthenticationException e)
                {
                    LogException.Message("InboundConnectionListener AuthenticationException", e);
                }
                catch (ObjectDisposedException ex)
                {
//                    Log.Message("*** AcceptConnections ObjectDisposedException from " + ListenerIpAddress + Environment.NewLine +ex);
                }
                catch (SocketException ex)
                {
                    switch (ex.Message)
                    {
                        case "An existing connection was forcibly closed by the remote host":
//                            Log.Message("*** AcceptConnections SocketException " + ListenerIpAddress + " closed the connection.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogException.Message("*** AcceptConnections Exception from ", ex);
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

            WaitHandle asyncClientWaitHandle = null;

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    IPEndPoint targetEndpoint;
                    try
                    {
                        targetEndpoint = EndpointBuilder.BuildNewEndPoint(ip, port);
                    }
                    catch (ArgumentNullException e)
                    {
                        LogException.Message("BuildOutBoundConnection: BuildNewEndPoint", e);
                        return;
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        LogException.Message("BuildOutBoundConnection: BuildNewEndPoint", e);
                        return;
                    }

                    IAsyncResult asyncClient;
                    try
                    {
                        asyncClient = tcpClient.BeginConnect(targetEndpoint.Address, targetEndpoint.Port, null, null);
                        asyncClientWaitHandle = asyncClient.AsyncWaitHandle;
                    }
                    catch (ArgumentNullException e)
                    {
                        LogException.Message("BuildOutBoundConnection: BeginConnect", e);
                        return;
                    }
                    catch (SocketException e)
                    {
                        LogException.Message("BuildOutBoundConnection: BeginConnect", e);
                        return;
                    }
                    catch (ObjectDisposedException e)
                    {
                        LogException.Message("BuildOutBoundConnection: BeginConnect", e);
                        return;
                    }
                    catch (SecurityException e)
                    {
                        LogException.Message("BuildOutBoundConnection: BeginConnect", e);
                        return;
                    }

                    try
                    {
                        if (!asyncClient.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                        {
                            tcpClient.Close();
                            throw new TimeoutException("Timeout connecting to " + targetEndpoint.Address + ":" +
                                                       targetEndpoint.Port);
                        }
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        LogException.Message("BuildOutBoundConnection: WaitOne", e);
                        return;
                    }
                    catch (ObjectDisposedException e)
                    {
                        LogException.Message("BuildOutBoundConnection: WaitOne", e);
                        return;
                    }
                    catch (AbandonedMutexException e)
                    {
                        LogException.Message("BuildOutBoundConnection: WaitOne", e);
                        return;
                    }
                    catch (InvalidOperationException e)
                    {
                        LogException.Message("BuildOutBoundConnection: WaitOne", e);
                        return;
                    }
                    catch (OverflowException e)
                    {
                        LogException.Message("BuildOutBoundConnection: FromSeconds", e);
                        return;
                    }
                    catch (ArgumentException e)
                    {
                        LogException.Message("BuildOutBoundConnection: FromSeconds", e);
                        return;
                    }

                    try
                    {
                        tcpClient.EndConnect(asyncClient);
                    }
                    catch (ArgumentNullException e)
                    {
                        LogException.Message("BuildOutBoundConnection: EndConnect", e);
                        return;
                    }
                    catch (ArgumentException e)
                    {
                        LogException.Message("BuildOutBoundConnection: EndConnect", e);
                        return;
                    }
                    catch (SocketException e)
                    {
                        LogException.Message("BuildOutBoundConnection: EndConnect", e);
                        return;
                    }
                    catch (ObjectDisposedException e)
                    {
                        LogException.Message("BuildOutBoundConnection: EndConnect", e);
                        return;
                    }
                    catch (InvalidOperationException e)
                    {
                        LogException.Message("BuildOutBoundConnection: EndConnect", e);
                        return;
                    }

                    Connection connection;
                    try
                    {
                        connection = StartPeerConnection(tcpClient);
                    }
                    catch (ArgumentNullException e)
                    {
                        LogException.Message("BuildOutBoundConnection: EndConnect", e);
                        return;
                    }
                    catch (ObjectDisposedException e)
                    {
                        LogException.Message("BuildOutBoundConnection: EndConnect", e);
                        return;
                    }
                    catch (InvalidOperationException e)
                    {
                        LogException.Message("BuildOutBoundConnection: EndConnect", e);
                        return;
                    }

                    try
                    {
                        connection = GetPeerConnectionTlsStream(connection, 2, targetEndpoint);
                    }
                    catch (ArgumentNullException e)
                    {
                        LogException.Message("BuildOutBoundConnection: EndConnect", e);
                        return;
                    }
                    catch (InvalidOperationException e)
                    {
                        LogException.Message("BuildOutBoundConnection: EndConnect", e);
                        return;
                    }

                    if (await DataReceiver(connection, Token)) return;
                    throw new Exception("*** FinalizeConnection unable to add peer " + connection.EndPoint.Address +
                                        connection.EndPoint.Port);
                }
            }
            catch (Exception e)
            {
                LogException.Message("BuildOutBoundConnection: GetPeerConnectionTlsStream", e);
            }
            finally
            {
                asyncClientWaitHandle?.Close();
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

            Connection connection;

            try
            {
                connection = new Connection(tcpClient);
            }
            catch (ArgumentNullException e)
            {
                LogException.Message("StartPeerConnection: Connection", e);
                throw;
            }
            catch (ObjectDisposedException e)
            {
                LogException.Message("StartPeerConnection: Connection", e);
                throw;
            }
            catch (InvalidOperationException e)
            {
                LogException.Message("StartPeerConnection: Connection", e);
                throw;
            }

            int activeCount;
            try
            {
                activeCount = Interlocked.Increment(ref ActiveConnections);
            }
            catch (NullReferenceException e)
            {
                LogException.Message("StartPeerConnection: Interlocked.Increment", e);
                throw;
            }

            Log.Message("*** Connection created for " + connection.EndPoint.Address + connection.EndPoint.Port +
                        " (now " + activeCount + " connections)");
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
                LogException.Message("DisconnectConnection: RemoveUnidentifiedConnectionFromList", e);
                return false;
            }
            finally
            {
                var activeCount = Interlocked.Decrement(ref ActiveConnections);
                Log.Message("***** Connection successfully disconnected connected (now " + activeCount +
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
            Log.Message("Catalyst.Helpers.Network class disposed");
        }
    }
}