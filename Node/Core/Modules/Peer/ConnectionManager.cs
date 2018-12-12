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
using System.Text;
using ADL.Hex.HexConvertors.Extensions;
using ADL.KeySigner;
using ADL.Node.Core.Modules.Peer.Messages;
using ADL.Protocol.Peer;
using ADL.RLP;
using ADL.Util;

namespace ADL.Node.Core.Modules.Peer
{
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionManager : IDisposable
    {
        private int _ActiveConnections;
        private bool _debug = false;
        private TcpListener _Listener;
        private bool Disposed = false;
        private SemaphoreSlim _SendLock;
        private bool AcceptInvalidCerts;
        private static ConnectionManager _instance;
        private CancellationToken _Token;
        private bool _MutuallyAuthenticate;
        private List<string> _PermittedIps;
        private CancellationTokenSource _TokenSource;
        private readonly  IPAddress _ListenerIpAddress;
        private readonly  int _ListenerPort;
        private X509Certificate2 _SslCertificate = null;
        private ConcurrentDictionary<string, ConnectionMeta> _Connections;
        private static readonly object Mutex = new object();
        private X509Certificate2Collection _SslCertificateCollection;
        
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
            
            if (_instance == null)
            {
                lock (Mutex)
                {
                    if (_instance == null)
                    {
                        // ms x509 facility generates invalid x590 certs (ofc ms!!!) have to accept invalid certs for now.
                        // @TODO revist this once we re-write the current ssl layer to use bouncy castle.
                        // @TODO revist permitted ips
                        //@TODO get debug value from what pass in at initialisation of application.
                        _instance = new ConnectionManager(
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
            return _instance;
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
        /// <param name="peerConnected"></param>
        /// <param name="peerDisconnected"></param>
        /// <param name="messageReceived"></param>
        /// <param name="debug"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ConnectionManager (
            IPeerSettings peerSettings,
            ISslSettings sslSettings,
            string dataDir,
            bool acceptInvalidCerts,
            bool mutualAuthentication,
            IEnumerable<string> permittedIps,
            bool debug)
        {
            if (peerSettings == null) throw new ArgumentNullException(nameof(peerSettings));
            if (sslSettings == null) throw new ArgumentNullException(nameof(sslSettings));
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));

            //@TODO maybe we need to extend this to stop it listening on privileged ports up to 1023
            if (peerSettings.Port < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(peerSettings.Port));
            }
            
            _debug = debug;
            AcceptInvalidCerts = acceptInvalidCerts;
            _MutuallyAuthenticate = mutualAuthentication;
            _ListenerPort = peerSettings.Port;
            
            if (permittedIps != null && permittedIps.Count() > 0)
            {
                _PermittedIps = new List<string>(permittedIps);
            }
            else
            {
                _ListenerIpAddress = IPAddress.Parse(peerSettings.BindAddress);

            }
            if (String.IsNullOrEmpty(sslSettings.SslCertPassword))
            {
                _SslCertificate = new X509Certificate2(dataDir+"/"+sslSettings.PfxFileName);
            }
            else
            {
                _SslCertificate = new X509Certificate2(dataDir+"/"+sslSettings.PfxFileName, sslSettings.SslCertPassword);
            }           
            
            _SslCertificateCollection = new X509Certificate2Collection
            {
                _SslCertificate
            };

            _ActiveConnections = 0;
            _Listener = new TcpListener(_ListenerIpAddress, _ListenerPort);
            _TokenSource = new CancellationTokenSource();
            _Token = _TokenSource.Token;
            _Connections = new ConcurrentDictionary<string, ConnectionMeta>();
            InboundConnectionListener();
//            Task.Run(async () => await InboundConnectionListener());
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
            
            if (!_Connections.TryRemove(connectionMeta.Ip+":"+connectionMeta.Port, out ConnectionMeta removedConnection))
            {
                // do nothing, it probably did not exist anyway
            }

            if (_Connections.TryAdd(connectionMeta.Ip+":"+connectionMeta.Port, connectionMeta))
            {
                int activeCount = Interlocked.Increment(ref _ActiveConnections);
                Log.Log.Message("*** FinalizeConnection starting data receiver for " + connectionMeta.Ip + connectionMeta.Port + " (now " + activeCount + " connections)");
            }
            else
            {
                connectionMeta.Dispose();
                return false;
            }
            Task.Run(async () => await DataReceiver(connectionMeta));
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionMeta"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private async Task DataReceiver(TcpClient tcpClient, CancellationToken? cancelToken=null)
        {
            //@TODO connectionmetta to tcp client
            var port = ((IPEndPoint)tcpClient.Client.LocalEndPoint).Port;
            var ip = ((IPEndPoint)tcpClient.Client.LocalEndPoint).Address.ToString();
            
            try
            {
                while (true)
                {
                    cancelToken?.ThrowIfCancellationRequested();

                    if (!IsConnected(tcpClient))
                    {
                        Log.Log.Message("*** Data receiver can not attach to connection");
                        break;
                    }

                    byte[] payload = await Stream.Reader.MessageReadAsync(connectionMeta);
                    // this causes dos
                    if (payload == null)
                    {
                        await Task.Delay(30);
                        // add counter to stop loop
                        continue;
                    }
                    
                    Task<string> unawaited = Task.Run(() =>
                    {
                        byte[] msgDescriptor = ByteUtil.Slice(payload, 0, 3);
                        byte[] message = ByteUtil.Slice(payload, 3);
                        Console.WriteLine(BitConverter.ToString(msgDescriptor));
                        Console.WriteLine(BitConverter.ToString(message));
                        return "process message in this task";
                    });
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
                Task<int> activeCount = Task.Run(() => DisconnectConnection(connectionMeta.Ip, connectionMeta.Port));
                Log.Log.Message("***** Successfully removed " + connectionMeta.Ip + connectionMeta.Port +
                                " connected (now " + activeCount + " connections active)");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task InboundConnectionListener()
        {
            _Listener.Start();
            Console.WriteLine(_Token.IsCancellationRequested);
            Log.Log.Message("Peer server starting on " + _ListenerIpAddress + ":" );
   
            //@TODO we need to announce our node to trackers.
            
            ConnectionMeta connectionMeta = null;
            
            while (!_Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient tcpPeer = await _Listener.AcceptTcpClientAsync();
                    tcpPeer.LingerState.Enabled = false;

                    string peerIp = ((IPEndPoint) tcpPeer.Client.RemoteEndPoint).Address.ToString();

                    if (_PermittedIps != null && _PermittedIps.Count > 0)
                    {
                        if (!_PermittedIps.Contains(peerIp))
                        {
                            Log.Log.Message("*** AcceptConnections rejecting connection from " + peerIp + " (not permitted)");
                            tcpPeer.Close();
                            continue;
                        }
                    }

                    // inbound peer
                    //do we want to elevate a new connection as peer immediatly?
                    connectionMeta = new ConnectionMeta(tcpPeer);

                    Log.Log.Message("*** AcceptConnections accepted connection from " + connectionMeta.Ip + connectionMeta.Port + " count " +
                        _ActiveConnections);


                    Task unawaited = Task.Run(async () =>
                    {
                        try
                        {

                            connectionMeta.SslStream = await Stream.StreamFactory.GetTlsStream(
                                connectionMeta.NetworkStream,
                                1,
                                _SslCertificate,
                                AcceptInvalidCerts,
                                false,
                                null
                            );                                
                            if (!AddConnection(connectionMeta))
                            {
                                Log.Log.Message("*** FinalizeConnection unable to add peer " + connectionMeta.Ip + connectionMeta.Port);
                                return;
                            }
                        }
                        catch (AuthenticationException e)
                        {
                           Log.LogException.Message("InboundConnectionListener AuthenticationException", e);
                           return;
                        }
                    }, _Token);
                }
                catch (ObjectDisposedException ex)
                {
                    // Listener stopped ? if so, peerIpPort will be empty
                    Log.Log.Message("*** AcceptConnections ObjectDisposedException from " + _ListenerIpAddress + Environment.NewLine +
                        ex.ToString());
                }
                catch (SocketException ex)
                {
                    switch (ex.Message)
                    {
                        case "An existing connection was forcibly closed by the remote host":
                            Log.Log.Message("*** AcceptConnections SocketException " + _ListenerIpAddress + " closed the connection.");
                            break;
                        default:
                            Log.Log.Message("*** AcceptConnections SocketException from " + _ListenerIpAddress + Environment.NewLine +
                                ex.ToString());
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Log.Message("*** AcceptConnections Exception from " + _ListenerIpAddress + Environment.NewLine + ex.ToString());
                }
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="pfxCertFile"></param>
        /// <param name="pfxCertPass"></param>
        /// <param name="acceptInvalidCerts"></param>
        /// <param name="mutualAuthentication"></param>
        /// <param name="debug"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        public async void PeerBuilder (string ip, int port)
        {
            if (ip == null) throw new ArgumentNullException(nameof(ip));
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));

            if (String.IsNullOrEmpty(ip))
            {
                throw new ArgumentNullException(nameof(ip));
            }

            if (port < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            
            _SendLock = new SemaphoreSlim(1);
            
            TcpClient tcpClient = new TcpClient();
            IAsyncResult ar = tcpClient.BeginConnect(ip, port, null, null);
            WaitHandle wh = ar.AsyncWaitHandle;
            ConnectionMeta connectionMeta = null;
            
            try
            {
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    tcpClient.Close();
                    throw new TimeoutException("Timeout connecting to " + ip + ":" + port);
                }

                tcpClient.EndConnect(ar);

                connectionMeta = new ConnectionMeta(tcpClient);

                connectionMeta.SslStream = await Stream.StreamFactory.GetTlsStream(
                    connectionMeta.NetworkStream,
                    2,
                    _SslCertificate,
                    AcceptInvalidCerts,
                    false,
                    null,
                    ip,
                    port
                );
                                
                Log.Log.Message("Trace22");
                if (!AddConnection(connectionMeta))
                {
                    Log.Log.Message("*** FinalizeConnection unable to add peer " + connectionMeta.Ip + connectionMeta.Port);
                    return;
                }
                Task.Run(async () => await DataReceiver(connectionMeta, _Token), _Token);
            }
            
            finally
            {
                Console.WriteLine("Starting Challenge Request");

                PeerProtocol.Types.ChallengeRequest requestMessage = MessageFactory.Get(2);

                SecureRandom random = new SecureRandom();
                byte[] keyBytes = new byte[16];
                random.NextBytes(keyBytes);
                requestMessage.Nonce = random.NextInt();
                connectionMeta.nonce = requestMessage.Nonce;
                byte[] requestBytes = requestMessage.ToByteArray();
                Console.WriteLine(requestMessage);
                Console.WriteLine(HexByteConvertorExtensions.ToHex(requestBytes));             
                await Stream.Writer.MessageWriteAsync(connectionMeta, requestBytes, 98, _SendLock);
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
                if ((tcpClient.Client.Poll(0, SelectMode.SelectWrite)) && (!tcpClient.Client.Poll(0, SelectMode.SelectError)))
                {
                    byte[] buffer = new byte[1];
                    if (tcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        /// Disconnects a connection
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private int DisconnectConnection(string ip, int port)
        {
            if (ip == null) throw new ArgumentNullException(nameof(ip));
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
            
            if (!_Connections.TryGetValue(ip+":"+port, out ConnectionMeta connection))
            {
                Log.Log.Message("*** Disconnect unable to find connection " + connection.Ip+":"+connection.Port);
                throw new Exception();
//                return false;
            }
  
            if (!_Connections.TryRemove(connection.Ip+":"+connection.Port, out ConnectionMeta removedPeer))
            {
                Log.Log.Message("*** RemovePeer unable to remove peer " + connection.Ip + connection.Port);
                throw new Exception();
                return false;
            }

            removedPeer.Dispose();            
            return Interlocked.Decrement(ref _ActiveConnections);
        }
        
        /// <summary>
        /// returns a list of our peers
        /// </summary>
        /// <returns></returns>
        public List<string> ListPeers()
        {
            Dictionary<string, ConnectionMeta> peers = _Connections.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
            
            return (_Connections.TryGetValue(ip+":"+port, out ConnectionMeta peer));
        }
        
        public void Dispose()
        {
            Console.WriteLine("disposing network class");
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// dispose server and background workers.
        /// </summary>
        public void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
                _TokenSource.Cancel();
                _TokenSource.Dispose();

                if (_Listener != null && _Listener.Server != null)
                {
                    _Listener.Server.Close();
                    _Listener.Server.Dispose();
                }

                if (_Connections != null && _Connections.Count > 0)
                {
                    foreach (KeyValuePair<string, ConnectionMeta> peer in _Connections)
                    {
                        peer.Value.Dispose();
                    }
                }
            }
            Disposed = true;            
        }
    }
}
