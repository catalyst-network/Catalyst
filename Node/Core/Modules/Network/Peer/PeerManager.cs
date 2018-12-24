using System;
using System.Collections.Concurrent;
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
using ADL.Node.Core.Modules.Network.Workers;
using ADL.Protocol.Peer;
using ADL.Util;
using Google.Protobuf;
using Org.BouncyCastle.Security;

namespace ADL.Node.Core.Modules.Network.Peer
{
    /// <summary>
    /// 
    /// </summary>
    public class PeerManager : IDisposable
    {
        private bool Disposed  { get; set; }
        private CancellationToken Token { get; }
        private TcpListener Listener { get; set; }
        private IPAddress ListenerIpAddress { get; }
        private bool AcceptInvalidCerts { get; set; }
        private List<string> BannedIps { get; set; } //@TODO revist this
        private X509Certificate2 SslCertificate { get; }
        private CancellationTokenSource CancellationToken { get; set; }
        private ConcurrentDictionary<string, Connection> UnIdentifiedConnections { get; set; }

        private int ActiveConnections;
        internal IWorkScheduler Worker;
        private readonly Queue<byte[]> SendMessageQueue;
        private readonly Queue<byte[]> ReceivedMessageQueue;

        public PeerManager(X509Certificate2 sslCertificate)
        {
            ActiveConnections = 0;
            SslCertificate = sslCertificate;
        }

        private void ProcessMessageQueue()//@TODO this is duplicated in message queue manager
        {
            Console.WriteLine("ProcessMessageQueue");
            lock (ReceivedMessageQueue)
            {
                Log.Log.Message("Messages to process: " + ReceivedMessageQueue.Count);
                byte[] msg = null;
                var receivedCount = ReceivedMessageQueue.Count;
                for (var i = 0; i < receivedCount; i++)
                {
                    Log.Log.Message("processing message: " + receivedCount);
                    msg = ReceivedMessageQueue.Dequeue();
                }
                byte[] msgDescriptor = msg.Slice(0, 3);
                byte[] message = msg.Slice(3);
                Console.WriteLine(BitConverter.ToString(msgDescriptor));
                Console.WriteLine(BitConverter.ToString(message));
            }
            Console.WriteLine("unlocked msg queue");
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
            Log.Log.Message("attempting to add connection");
            var port = ((IPEndPoint) connection.TcpClient.Client.LocalEndPoint).Port;
            var ip = ((IPEndPoint) connection.TcpClient.Client.LocalEndPoint).Address.ToString();

            if (connection == null) throw new ArgumentNullException(nameof(connection));
            
            if (UnIdentifiedConnections.TryRemove(ip+":"+port, out Connection removedConnection))
            {
                Log.Log.Message(removedConnection + "Connection already exists");
                return false;
            }

            // @TODO we need to get this active connections incrementer out this method, then this DataReceiver method needs to go somewhere related to messages.
            if (UnIdentifiedConnections.TryAdd(ip+":"+port, connection))
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

                    if (!IsConnected(connection.TcpClient))
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
                        lock (ReceivedMessageQueue)
                        {
                            ReceivedMessageQueue.Enqueue(payload);
                            Log.Log.Message("messages in queue: " + ReceivedMessageQueue.Count);
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
        internal async Task InboundConnectionListener()
        {
            Listener.Start();
//            Worker.QueueForever(ProcessMessageQueue, TimeSpan.FromMilliseconds(2000));
//            Worker.Start();
            Console.WriteLine(Token.IsCancellationRequested);
            Log.Log.Message("Peer server starting on " + ListenerIpAddress + ":" );
   
            //@TODO we need to announce our node to trackers.

            while (!Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient tcpPeer = await Listener.AcceptTcpClientAsync();
                    tcpPeer.LingerState.Enabled = false;

                    string peerIp = ((IPEndPoint) tcpPeer.Client.RemoteEndPoint).Address.ToString();

                    //@TODO revist this
//                    if (BannedIps?.Count > 0)
//                    {
//                        if (!BannedIps.Contains(peerIp))
//                        {
//                            Log.Log.Message("*** AcceptConnections rejecting connection from " + peerIp + " (not permitted)");
//                            tcpPeer.Close();
//                            continue;
//                        }
//                    }

                    // inbound peer
                    //do we want to elevate a new connection as peer immediatly?
                    var connection = new Connection(tcpPeer);

                    Log.Log.Message("*** AcceptConnections accepted connection from " + connection.Ip + connection.Port + " count " + ActiveConnections);

                    connection.SslStream = Stream.StreamFactory.CreateTlsStream(
                        connection.NetworkStream,
                            1,
                            SslCertificate,
                            AcceptInvalidCerts
                        );
                    
                    if (connection.SslStream == null || connection.SslStream.GetType() != typeof(SslStream))
                    {
                        throw new Exception("Peer ssl stream not set");
                    }

                    if (await DataReceiver(connection, Token))
                    {
                        Console.WriteLine("Starting Challenge Request");
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
                    Log.Log.Message("*** AcceptConnections ObjectDisposedException from " + ListenerIpAddress + Environment.NewLine +ex);
                }
                catch (SocketException ex)
                {
                    switch (ex.Message)
                    {
                        case "An existing connection was forcibly closed by the remote host":
                            Log.Log.Message("*** AcceptConnections SocketException " + ListenerIpAddress + " closed the connection.");
                            break;
                        default:
                            Log.Log.Message("*** AcceptConnections SocketException from " + ListenerIpAddress + Environment.NewLine + ex);
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
            if (port < 1024) throw new ArgumentOutOfRangeException(nameof(port));
            if (string.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));

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
            if (port < 1024) throw new ArgumentOutOfRangeException(nameof(port));
            
            if (!UnIdentifiedConnections.TryGetValue(ip+":"+port, out Connection connection))
            {
                Log.Log.Message("*** Disconnect unable to find connection " + ip+":"+port);
                throw new Exception();
            }
            if (!UnIdentifiedConnections.TryRemove(connection.Ip+":"+connection.Port, out Connection removedPeer))
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
        /// returns a list of our peers
        /// </summary>
        /// <returns></returns>
        public List<string> ListPeers()
        {
            Dictionary<string, Connection> peers = UnIdentifiedConnections.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            List<string> ret = new List<string>();
            foreach (KeyValuePair<string, Connection> curr in peers)
            {
                Console.WriteLine(curr.Key);
                ret.Add(curr.Key);
            }
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool IsPeerConnected(string ip, int port)
        {
            if (ip == null) throw new ArgumentNullException(nameof(ip));
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
            
            return UnIdentifiedConnections.TryGetValue(ip+":"+port, out Connection peer);
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            Console.WriteLine("disposing network class");
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
                if (UnIdentifiedConnections?.Count > 0)
                {
                    foreach (KeyValuePair<string, Connection> peer in UnIdentifiedConnections)
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
