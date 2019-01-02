using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ADL.Hex.HexConvertors.Extensions;
using ADL.Network;
using ADL.Node.Core.Modules.Network.Listeners;
using ADL.Node.Core.Modules.Network.Messages;
using ADL.Protocol.Peer;
using Google.Protobuf;
using Org.BouncyCastle.Security;

namespace ADL.Node.Core.Modules.Network.Peer
{
    /// <summary>
    /// 
    /// </summary>
    public class PeerManager : IDisposable
    {
        private int ActiveConnections;
        private bool Disposed  { get; set; }
        private PeerList PeerList  { get; set; }
        private TcpListener Listener { get; set; }
        private CancellationToken Token { get; set; }
        private bool AcceptInvalidCerts { get; set; }
        private X509Certificate2 SslCertificate { get; set; }
        private MessageQueueManager MessageQueueManager { get; set; }
        private CancellationTokenSource CancellationToken { get; set; }

        public PeerManager(X509Certificate2 sslCertificate, PeerList peerList, MessageQueueManager messageQueueManager)
        {
            PeerList = peerList;
            ActiveConnections = 0;
            SslCertificate = sslCertificate;
            MessageQueueManager = messageQueueManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private async Task<bool> DataReceiver(Connection connection, CancellationToken cancelToken)
        {
            var streamReadCounter = 0;
            var port = ((IPEndPoint) connection.TcpClient.Client.LocalEndPoint).Port;
            var ip = ((IPEndPoint) connection.TcpClient.Client.LocalEndPoint).Address.ToString();

            if (connection == null) throw new ArgumentNullException(nameof(connection));
            
            if (PeerList.UnIdentifiedPeers.TryRemove(ip+":"+port, out Connection removedConnection))
            {
                Log.Log.Message(removedConnection + "Connection already exists");
                return false;
            }

            if (PeerList.UnIdentifiedPeers.TryAdd(ip+":"+port, connection))
            {
                int activeCount = Interlocked.Increment(ref ActiveConnections);
                Log.Log.Message("*** FinalizeConnection starting data receiver for " + ip + port + " (now " + activeCount + " connections)");
            }
            else
            {
                connection.Dispose();
                return false;
            }
            
            try
            {
                while (true)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    if (!connection.IsConnected())
                    {
                        Log.Log.Message("*** Data receiver can not attach to connection");
                        break;
                    }

                    byte[] payload = Stream.Reader.MessageRead(connection.SslStream);

                    if (payload == null)
                    {
                        await Task.Delay(30, Token);
                        streamReadCounter += streamReadCounter;
                        // count how many times we try reading header && content so we don't get stuck in here.
                        if (streamReadCounter == 5)
                        {
                            break;
                        }
                    }
                    else
                    {
                        lock (MessageQueueManager._receivedMessageQueue)
                        {
                            MessageQueueManager._receivedMessageQueue.Enqueue(payload);
                            Log.Log.Message("messages in queue: " + MessageQueueManager._receivedMessageQueue.Count);
                        }
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                Log.LogException.Message("*** Data receiver cancelled " + ip + ":" + port + " disconnected", e);
                throw;
            }
            catch (Exception e)
            {
                Log.LogException.Message("*** Data receiver exception " + ip + ":" + port + " disconnected", e);
                throw;
            }
            finally
            {                
                await Task.Run(() => DisconnectConnection(connection.Ip, connection.Port), Token);
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal async Task InboundConnectionListener(IPEndPoint ipEndPoint)
        {
            TcpListener Listener = ListenerFactory.CreateTcpListener(ipEndPoint);

            Listener.Start();
//            Log.Log.Message("Peer server starting on " + ListenerIpAddress + ":" );
   
            //@TODO we need to announce our node to trackers.

            while (!Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient tcpPeer = await Listener.AcceptTcpClientAsync();
                    tcpPeer.LingerState.Enabled = false;

                    //@TODO revist this
//                    string peerIp = ((IPEndPoint) tcpPeer.Client.RemoteEndPoint).Address.ToString();
//
//                    if (BannedIps?.Count > 0)
//                    {
//                        if (!BannedIps.Contains(peerIp))
//                        {
//                            Log.Log.Message("*** AcceptConnections rejecting connection from " + peerIp + " (not permitted)");
//                            tcpPeer.Close();
//                            continue;
//                        }
//                    }

                    Connection connection;
                    try
                    {
                        connection = new Connection(tcpPeer);
                    }
                    catch (Exception e)
                    {
                        Log.LogException.Message("InboundConnectionListener", e);
                        return;
                    }

                    connection.SslStream = Stream.StreamFactory.CreateTlsStream(
                        connection.NetworkStream,
                            1,
                            SslCertificate,
                            AcceptInvalidCerts
                        );
                    
                    if (connection.SslStream == null || connection.SslStream.GetType() != typeof (SslStream))
                    {
                        throw new Exception("Peer ssl stream not set");
                    }

                    if (await DataReceiver(connection, Token))
                    {
                        Log.Log.Message("*** AcceptConnections accepted connection from " + connection.Ip + connection.Port + " count " + ActiveConnections);
                        Log.Log.Message("Starting Challenge Request");
                        PeerProtocol.Types.ChallengeRequest requestMessage = MessageFactory.Get(2);

                        SecureRandom random = new SecureRandom();
                        byte[] keyBytes = new byte[16];
                        random.NextBytes(keyBytes);
                        requestMessage.Nonce = random.NextInt();
                        if (connection.SslStream != null)
                        {
//                            connection.Nonce = requestMessage.Nonce;
                            byte[] requestBytes = requestMessage.ToByteArray();
                            Console.WriteLine(requestMessage);
                            Console.WriteLine(requestBytes.ToHex());
                            Stream.Writer.MessageWrite(connection, requestBytes, 98);
                        }
                        continue;
                    }
                    Log.Log.Message("*** FinalizeConnection unable to add peer " + connection.Ip + connection.Port);
                    throw new Exception("unable to add connection as peer");
                }
                catch (AuthenticationException e)
                {
                    Log.LogException.Message("InboundConnectionListener AuthenticationException", e);
                }
                catch (ObjectDisposedException ex)
                {
//                    Log.Log.Message("*** AcceptConnections ObjectDisposedException from " + ListenerIpAddress + Environment.NewLine +ex);
                }
                catch (SocketException ex)
                {
                    switch (ex.Message)
                    {
                        case "An existing connection was forcibly closed by the remote host":
//                            Log.Log.Message("*** AcceptConnections SocketException " + ListenerIpAddress + " closed the connection.");
                            break;
                        default:
//                            Log.Log.Message("*** AcceptConnections SocketException from " + peerIp.Ip + Environment.NewLine + ex);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.LogException.Message("*** AcceptConnections Exception from ", ex);
                }
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        public async void PeerBuilder (string ip, int port)
        {
            if (string.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
            if (Ip.ValidPortRange(port)) throw new ArgumentOutOfRangeException(nameof(port));

            try
            {
                using (TcpClient tcpClient = new TcpClient())
                { 
                    try
                    {
                        IPEndPoint targetEndpoint = EndpointBuilder.BuildNewEndPoint(ip, port);
                        IAsyncResult asyncClient = tcpClient.BeginConnect(targetEndpoint.Address, targetEndpoint.Port, null, null);
                        WaitHandle asyncClientWaitHandle = asyncClient.AsyncWaitHandle;
                        try
                        {
                            if (!asyncClient.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                            {
                                tcpClient.Close();
                                throw new TimeoutException("Timeout connecting to " +  targetEndpoint.Address + ":" + targetEndpoint.Port);
                            }

                            tcpClient.EndConnect(asyncClient);

                            var connection = new Connection(tcpClient);

                            connection.SslStream = Stream.StreamFactory.CreateTlsStream(
                                connection.NetworkStream,
                                2,
                                SslCertificate,
                                AcceptInvalidCerts,
                                false,
                                targetEndpoint
                            );
                
                            if (connection.SslStream == null || connection.SslStream.GetType() != typeof(SslStream))
                            {
                                throw new Exception("Peer ssl stream not set");
                            }

                            if (await DataReceiver(connection, Token)) return;
                            throw new Exception("*** FinalizeConnection unable to add peer " + connection.Ip + connection.Port);
                        }
                        catch (AuthenticationException e)
                        {
                            Log.LogException.Message("Peer builder socket exception", e);
                        }
                        finally
                        {
                            asyncClientWaitHandle.Close();
                        }
                    }
                    catch (ArgumentNullException e)
                    {
                        Log.LogException.Message("ADL.Node.Core.Modules.Network.Peer.PeerManager.PeerBuilder", e);
                    }
                }
            }
            catch (ArgumentException e)
            {
                Log.LogException.Message("ADL.Node.Core.Modules.Network.Peer.PeerManager.PeerBuilder", e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        private bool IsConnected(TcpClient tcpClient)
        {
            if (tcpClient == null) throw new ArgumentNullException(nameof(tcpClient));
            if (!tcpClient.Connected) return false;
            if (!tcpClient.Client.Poll(0, SelectMode.SelectWrite) ||
                tcpClient.Client.Poll(0, SelectMode.SelectError)) return false;
            byte[] buffer = new byte[1];
            return tcpClient.Client.Receive(buffer, SocketFlags.Peek) != 0;
        }
        
        /// <summary>
        /// Disconnects a connection
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private bool DisconnectConnection(string ip, int port)
        {
            if (ip == null) throw new ArgumentNullException(nameof(ip));
            if (Ip.ValidPortRange(port)) throw new ArgumentOutOfRangeException(nameof(port));
            
            if (!PeerList.UnIdentifiedPeers.TryGetValue(ip+":"+port, out Connection connection))
            {
                Log.Log.Message("*** Disconnect unable to find connection " + ip+":"+port);
                throw new Exception();
            }
            if (!PeerList.UnIdentifiedPeers.TryRemove(connection.Ip+":"+connection.Port, out Connection removedPeer))
            {
                Log.Log.Message("*** RemovePeer unable to remove peer " + connection.Ip + connection.Port);
                throw new Exception();
            }

            removedPeer.Dispose();
            var activeCount = Interlocked.Decrement(ref ActiveConnections);
            Log.Log.Message("***** Successfully removed " + ip + port +" connected (now " + activeCount + " connections active)");

            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            Log.Log.Message("disposing network class");
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// dispose server and background workers.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

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
                {
                    foreach (KeyValuePair<string, Connection> peer in PeerList.UnIdentifiedPeers)
                    {
                        peer.Value.Dispose();
                    }
                }
                
                if (PeerList._peerList?.Count > 0)
                {
                    foreach (KeyValuePair<PeerIdentifier, Peer> peer in PeerList._peerList)
                    {
                        peer.Value.Dispose();
                    }
                }
            }
            
            Disposed = true;    
            Log.Log.Message("Network class disposed");
        }
    }
}
