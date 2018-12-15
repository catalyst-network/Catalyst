using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Org.BouncyCastle.Security;
using ADL.Hex.HexConvertors.Extensions;
using ADL.Node.Core.Modules.Peer.Messages;
using ADL.Node.Core.Modules.Peer.Workers;
using ADL.Protocol.Peer;
using ADL.Util;

namespace ADL.Node.Core.Modules.Peer
{
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionManager : IDisposable
    {
        private bool Debug { get; set; }
        private bool Disposed  { get; set; }
        private int ListenerPort { get; set; }
        private CancellationToken Token { get; }
        private TcpListener Listener { get; set; }
        private IPAddress ListenerIpAddress { get; }
        private SemaphoreSlim SendLock { get; set; }
        private bool AcceptInvalidCerts { get; set; }
        private bool MutuallyAuthenticate { get; set; }
        private List<string> PermittedIps { get; set; }
        private X509Certificate2 SslCertificate { get; }
        private static ConnectionManager Instance { get; set; }
        private CancellationTokenSource TokenSource { get; set; }
        private X509Certificate2Collection SslCertificateCollection { get; set; }
        private ConcurrentDictionary<string, ConnectionMeta> Connections { get; set; }

        internal IWorkScheduler Worker;
        private int ActiveConnections;
        private readonly Queue<byte[]> SendMessageQueue;
        private readonly Queue<byte[]> ReceivedMessageQueue;
        private static readonly object Mutex = new object();
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        public static ConnectionManager GetInstance(IPeerSettings peerSettings, ISslSettings sslSettings, string dataDir)
        {
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));
            if (sslSettings == null) throw new ArgumentNullException(nameof(sslSettings));
            if (peerSettings == null) throw new ArgumentNullException(nameof(peerSettings));
            
            if (Instance == null)
            {
                lock (Mutex)
                {
                    if (Instance == null)
                    {
                        // ms x509 facility generates invalid x590 certs (ofc ms!!!) have to accept invalid certs for now.
                        // @TODO revist this once we re-write the current ssl layer to use bouncy castle.
                        // @TODO revist permitted ips
                        //@TODO get debug value from what pass in at initialisation of application.
                        Instance = new ConnectionManager(
                            peerSettings,
                            sslSettings,
                            dataDir,
                            true,
                            false,
                            null,
                            true
                        );
                    }
                }
            }
            return Instance;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <param name="acceptInvalidCerts"></param>
        /// <param name="mutualAuthentication"></param>
        /// <param name="permittedIps"></param>
        /// <param name="debug"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private ConnectionManager (
            IPeerSettings peerSettings,
            ISslSettings sslSettings,
            string dataDir,
            bool acceptInvalidCerts,
            bool mutualAuthentication,
            IEnumerable<string> permittedIps,
            bool debug)
        {
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));
            if (sslSettings == null) throw new ArgumentNullException(nameof(sslSettings));
            if (peerSettings == null) throw new ArgumentNullException(nameof(peerSettings));
            
            //dont let it run on privileged ports
            if (peerSettings.Port < 1024)
            {
                throw new ArgumentOutOfRangeException(nameof(peerSettings.Port));
            }
            
            Debug = debug;
            ListenerPort = peerSettings.Port;
            AcceptInvalidCerts = acceptInvalidCerts;
            MutuallyAuthenticate = mutualAuthentication;

            if (PermittedIps?.Count > 0)
            {
                PermittedIps = new List<string>(PermittedIps);
            }
            else
            {
                ListenerIpAddress = IPAddress.Parse(peerSettings.BindAddress);
            }
            if (String.IsNullOrEmpty(sslSettings.SslCertPassword))
            {
                SslCertificate = new X509Certificate2(dataDir+"/"+sslSettings.PfxFileName);
            }
            else
            {
                SslCertificate = new X509Certificate2(dataDir+"/"+sslSettings.PfxFileName, sslSettings.SslCertPassword);
            }           
            
            SslCertificateCollection = new X509Certificate2Collection
            {
                SslCertificate
            };

            ActiveConnections = 0;
            Worker = new ClientWorker();
            SendMessageQueue = new Queue<byte[]>();
            ReceivedMessageQueue = new Queue<byte[]>();
            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;
            Listener = new TcpListener(ListenerIpAddress, ListenerPort);
            Connections = new ConcurrentDictionary<string, ConnectionMeta>();
            Task.Run(async () => await InboundConnectionListener());
//            InboundConnectionListener();
        }

        private void ProcessMessageQueue()
        {
            Console.WriteLine("ProcessMessageQueue");
            Console.WriteLine(ReceivedMessageQueue.Count);
            Console.WriteLine(ReceivedMessageQueue.ToString());

            lock (ReceivedMessageQueue)
            {
                byte[] msg = null;
                var receivedCount = ReceivedMessageQueue.Count;
                Log.Log.Message("Messages to process: " + receivedCount);
                for (var i = 0; i < receivedCount; i++)
                {
                    Log.Log.Message("processing message: " + receivedCount);
                    msg = ReceivedMessageQueue.Dequeue();
                }
                byte[] msgDescriptor = ByteUtil.Slice(msg, 0, 3);
                byte[] message = ByteUtil.Slice(msg, 3);
                Console.WriteLine(BitConverter.ToString(msgDescriptor));
                Console.WriteLine(BitConverter.ToString(message));
            }
            Console.WriteLine("unlocked msg queue");
        }
                
        /// <summary>
        /// TODO 
        /// </summary>
        /// <param name="connectionMeta"></param>
        /// <returns></returns>
        private bool AddConnection(ConnectionMeta connectionMeta)
        {
            Log.Log.Message("attempting to add connection");
            if (connectionMeta == null) throw new ArgumentNullException(nameof(connectionMeta));
            
            if (Connections.TryRemove(connectionMeta.Ip+":"+connectionMeta.Port, out ConnectionMeta removedConnection))
            {
                Log.Log.Message(removedConnection + "Connection already exists");
                return false;
            }

            if (Connections.TryAdd(connectionMeta.Ip+":"+connectionMeta.Port, connectionMeta))
            {
                int activeCount = Interlocked.Increment(ref ActiveConnections);
                Log.Log.Message("*** FinalizeConnection starting data receiver for " + connectionMeta.Ip + connectionMeta.Port + " (now " + activeCount + " connections)");
            }
            else
            {
                connectionMeta.Dispose();
                return false;
            }
            Task.Run(async () => await DataReceiver(connectionMeta), Token);
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionMeta"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private async Task DataReceiver(ConnectionMeta connectionMeta, CancellationToken? cancelToken=null)
        {
            var streamReadCounter = 0;
            var port = ((IPEndPoint)connectionMeta.TcpClient.Client.LocalEndPoint).Port;
            var ip = ((IPEndPoint)connectionMeta.TcpClient.Client.LocalEndPoint).Address.ToString();
            
            try
            {
                while (true)
                {
                    cancelToken?.ThrowIfCancellationRequested();

                    if (!IsConnected(connectionMeta.TcpClient))
                    {
                        Log.Log.Message("*** Data receiver can not attach to connection");
                        break;
                    }

                    byte[] payload = Stream.Reader.MessageRead(connectionMeta.SslStream);

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
                        continue;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw; //normal cancellation
            }
            catch (Exception e)
            {
                Log.Log.Message("*** Data receiver exception " + ip + ":" + port + " disconnected");
                Log.LogException.Message("DataReceiver",e);
            }
            finally
            {                
                await Task.Run(() => DisconnectConnection(connectionMeta.Ip, connectionMeta.Port), Token);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task InboundConnectionListener()
        {
            Listener.Start();
            Worker.QueueForever(ProcessMessageQueue, TimeSpan.FromMilliseconds(2000));
            Worker.Start();
            Console.WriteLine(Token.IsCancellationRequested);
            Log.Log.Message("Peer server starting on " + ListenerIpAddress + ":" );
   
            //@TODO we need to announce our node to trackers.

            while (!Token.IsCancellationRequested)
            {
                ConnectionMeta connectionMeta = null;
                try
                {
                    TcpClient tcpPeer = await Listener.AcceptTcpClientAsync();
                    tcpPeer.LingerState.Enabled = false;

                    string peerIp = ((IPEndPoint) tcpPeer.Client.RemoteEndPoint).Address.ToString();

                    if (PermittedIps?.Count > 0)
                    {
                        if (!PermittedIps.Contains(peerIp))
                        {
                            Log.Log.Message("*** AcceptConnections rejecting connection from " + peerIp + " (not permitted)");
                            tcpPeer.Close();
                            continue;
                        }
                    }

                    // inbound peer
                    //do we want to elevate a new connection as peer immediatly?
                    connectionMeta = new ConnectionMeta(tcpPeer);

                    Log.Log.Message("*** AcceptConnections accepted connection from " + connectionMeta.Ip + connectionMeta.Port + " count " + ActiveConnections);

                    connectionMeta.SslStream = Stream.StreamFactory.GetTlsStream(
                        connectionMeta.NetworkStream,
                            1,
                            SslCertificate,
                            AcceptInvalidCerts
                        );
                    
                    if (connectionMeta.SslStream == null || connectionMeta.SslStream.GetType() != typeof(SslStream))
                    {
                        throw new Exception("Peer ssl stream not set");
                    }

                    if (AddConnection(connectionMeta))
                    {
                        Console.WriteLine("Starting Challenge Request");
                        PeerProtocol.Types.ChallengeRequest requestMessage = MessageFactory.Get(2);

                        SecureRandom random = new SecureRandom();
                        byte[] keyBytes = new byte[16];
                        random.NextBytes(keyBytes);
                        requestMessage.Nonce = random.NextInt();
                        if (connectionMeta?.SslStream != null)
                        {
                            connectionMeta.Nonce = requestMessage.Nonce;
                            byte[] requestBytes = requestMessage.ToByteArray();
                            Console.WriteLine(requestMessage);
                            Console.WriteLine(requestBytes.ToHex());
                            Stream.Writer.MessageWrite(connectionMeta, requestBytes, 98, SendLock);
                        }
                        continue;
                    }
                    Log.Log.Message("*** FinalizeConnection unable to add peer " + connectionMeta.Ip + connectionMeta.Port);
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
        public void PeerBuilder (string ip, int port)
        {
            if (ip == null) throw new ArgumentNullException(nameof(ip));
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));

            if (string.IsNullOrEmpty(ip))
            {
                throw new ArgumentNullException(nameof(ip));
            }
            if (port < 1024)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            
            SendLock = new SemaphoreSlim(1);
            
            ConnectionMeta connectionMeta = null;
            TcpClient tcpClient = new TcpClient();
            IAsyncResult ar = tcpClient.BeginConnect(ip, port, null, null);
            WaitHandle wh = ar.AsyncWaitHandle;
            
            try
            {
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    tcpClient.Close();
                    throw new TimeoutException("Timeout connecting to " + ip + ":" + port);
                }

                tcpClient.EndConnect(ar);

                connectionMeta = new ConnectionMeta(tcpClient);

                connectionMeta.SslStream = Stream.StreamFactory.GetTlsStream(
                    connectionMeta.NetworkStream,
                    2,
                    SslCertificate,
                    AcceptInvalidCerts,
                    false,
                    ip,
                    port
                );
                
                if (connectionMeta.SslStream == null || connectionMeta.SslStream.GetType() != typeof(SslStream))
                {
                    throw new Exception("Peer ssl stream not set");
                }

                if (AddConnection(connectionMeta)) return;
                throw new Exception("*** FinalizeConnection unable to add peer " + connectionMeta.Ip + connectionMeta.Port);
            }
            catch (AuthenticationException e)
            {
                Log.LogException.Message("Peer builder socket exception", e);
            }
            finally
            {
                wh.Close();
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
            
            if (tcpClient.Connected)
            {
                if (tcpClient.Client.Poll(0, SelectMode.SelectWrite) && !tcpClient.Client.Poll(0, SelectMode.SelectError))
                {
                    byte[] buffer = new byte[1];
                    if (tcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
                    {
                        return false;
                    }
                    return true;
                }
                return false;
            }
            return false;
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
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
            
            if (!Connections.TryGetValue(ip+":"+port, out ConnectionMeta connection))
            {
                Log.Log.Message("*** Disconnect unable to find connection " + connection.Ip+":"+connection.Port);
                throw new Exception();
            }
            if (!Connections.TryRemove(connection.Ip+":"+connection.Port, out ConnectionMeta removedPeer))
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
            Dictionary<string, ConnectionMeta> peers = Connections.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            List<string> ret = new List<string>();
            foreach (KeyValuePair<string, ConnectionMeta> curr in peers)
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
            
            return Connections.TryGetValue(ip+":"+port, out ConnectionMeta peer);
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
                TokenSource.Cancel();
                TokenSource.Dispose();

                if (Listener?.Server != null)
                {
                    Listener.Server.Close();
                    Listener.Server.Dispose();
                }
                if (Connections?.Count > 0)
                {
                    foreach (KeyValuePair<string, ConnectionMeta> peer in Connections)
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
