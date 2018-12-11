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
    public class Network : IDisposable
    {
        private int _ActivePeers;
        private bool _debug = false;
        private TcpListener _Listener;
        private bool Disposed = false;
        private SemaphoreSlim _SendLock;
        private bool AcceptInvalidCerts;
        private static Network _instance;
        private CancellationToken _Token;
        private bool _MutuallyAuthenticate;
        private List<string> _PermittedIps;
        private CancellationTokenSource _TokenSource;
        private readonly  IPAddress _ListenerIpAddress;
        private X509Certificate2 _SslCertificate = null;
        private ConcurrentDictionary<string, Peer> _Peers;
        private static readonly object Mutex = new object();
        private X509Certificate2Collection _SslCertificateCollection;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        public static Network GetInstance(IPeerSettings peerSettings, ISslSettings sslSettings, string dataDir)
        {
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
                        _instance = new Network(
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
        public Network (
            IPeerSettings peerSettings,
            ISslSettings sslSettings,
            string dataDir,
            bool acceptInvalidCerts,
            bool mutualAuthentication,
            IEnumerable<string> permittedIps,
            bool debug)
        {
            //@TODO maybe we need to extend this to stop it listening on privileged ports up to 1023
            if (peerSettings.Port < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(peerSettings.Port));
            }
            
            _debug = debug;
            AcceptInvalidCerts = acceptInvalidCerts;
            _MutuallyAuthenticate = mutualAuthentication;
            
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

            Log.Log.Message("Peer server starting on " + _ListenerIpAddress + ":" + peerSettings.Port);

            _ActivePeers = 0;
            _Listener = new TcpListener(_ListenerIpAddress, peerSettings.Port);
            _TokenSource = new CancellationTokenSource();
            _Token = _TokenSource.Token;
            _Peers = new ConcurrentDictionary<string, Peer>();

            Task.Run(async () => await InboundConnectionListener());
        }
        
        /// <summary>
        /// inbound connections = 1, outbound connections = 2
        /// </summary>
        /// <param name="sslStream"></param>
        /// <param name="port"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        /// <exception cref="AuthenticationException"></exception>
        private async Task<bool> StartTlsStream(Peer peer, int direction)
        {
            if (AcceptInvalidCerts)
            {
                peer.SslStream = new SslStream(peer.NetworkStream, false, new RemoteCertificateValidationCallback(AcceptCertificate));
            }
            else
            {
                // do not accept invalid SSL certificates
                peer.SslStream = new SslStream(peer.NetworkStream, false);
            }
            
            try
            {
                if (direction == 1)
                {
                    await peer.SslStream.AuthenticateAsServerAsync(_SslCertificate, true, SslProtocols.Tls12, false);                    
                } 
                else if (direction == 2)
                {
                    await peer.SslStream.AuthenticateAsClientAsync(peer.Ip, _SslCertificateCollection, SslProtocols.Tls12, !AcceptInvalidCerts);
                }
                if (!peer.SslStream.IsEncrypted)
                {
                    peer.Dispose();
                    throw new AuthenticationException();
                }
                if (!peer.SslStream.IsAuthenticated)
                {
                    Log.Log.Message("*** StartOutboundTls stream from " + peer.Ip + peer.Port + " not authenticated");
                    peer.Dispose();
                    throw new AuthenticationException();
                }
                if (_MutuallyAuthenticate && !peer.SslStream.IsMutuallyAuthenticated)
                {
                    Log.Log.Message("*** StartOutboundTls stream from " + peer.Ip + peer.Port + " failed mutual authentication");
                    peer.Dispose();
                    throw new AuthenticationException();
                }
            }
            catch (IOException ex)
            {
                // Some type of problem initiating the SSL connection
                switch (ex.Message)
                {
                    case "Authentication failed because the remote party has closed the transport stream.":
                    case "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.":
                        Log.Log.Message("*** StartTls IOException " + peer.Ip + peer.Port + " closed the connection.");
                        break;
                    case "The handshake failed due to an unexpected packet format.":
                        Log.Log.Message("*** StartTls IOException " + peer.Ip + peer.Port + " disconnected, invalid handshake.");
                        break;
                    default:
                        Log.Log.Message("*** StartTls IOException from " + peer.Ip + peer.Port + Environment.NewLine + ex.ToString());
                        break;
                }

                peer.Dispose();
                return false;
            }
            catch (Exception ex)
            {
                Log.Log.Message("*** StartInboundTls Exception from " + peer.Ip + peer.Port +  Environment.NewLine + ex.ToString());
                peer.Dispose();
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// TODO 
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        private bool AddPeer(Peer peer)
        {
            if (!_Peers.TryRemove(peer.Ip+":"+peer.Port, out Peer removedPeer))
            {
                // do nothing, it probably did not exist anyway
            }

            if (_Peers.TryAdd(peer.Ip+":"+peer.Port, peer))
            {
                int activeCount = Interlocked.Increment(ref _ActivePeers);
                Log.Log.Message("*** FinalizeConnection starting data receiver for " + peer.Ip + peer.Port + " (now " + activeCount + " peers)");
            }
            else
            {
                peer.Dispose();
                return false;
            }
            Task.Run(async () => await DataReceiver(peer));
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private async Task DataReceiver(Peer peer, CancellationToken? cancelToken=null)
        {
            var port = ((IPEndPoint)peer.TcpClient.Client.LocalEndPoint).Port;
            var ip = ((IPEndPoint)peer.TcpClient.Client.LocalEndPoint).Address.ToString();
            
            try
            {
                while (true)
                {
                    cancelToken?.ThrowIfCancellationRequested();

                    if (!IsConnected(peer))
                    {
                        Log.Log.Message("*** DataReceiver null TCP interface detected, disconnection or close assumed");
                        break;
                    }

                    byte[] payload = await Stream.Reader.MessageReadAsync(peer);
                    if (payload == null)
                    {
                        await Task.Delay(30);
                        continue;
                    }
                    
                    Task<string> unawaited = Task.Run(() =>
                    {
                        byte[] msgDescriptor = ByteUtil.Slice(payload, 0, 3);
                        byte[] message = ByteUtil.Slice(payload, 3);
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
                Log.Log.Message("*** OutboundChannelListener exception server " + ip + ":" + port + " disconnected");
                Log.LogException.Message("OutboundChannelListener",e);
            }
            finally
            {
                int activeCount = Interlocked.Decrement(ref _ActivePeers);
                
                Task<bool> success = Task.Run(() => DisconnectPeer(peer.Ip, peer.Port));
                
                if (success.Result)
                {
                    Log.Log.Message("***** DataReceiver peer " + peer.Ip + peer.Port + " disconnected (now " + activeCount + " peers active)");                    
                }
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
            
            //@TODO we need to announce our node to trackers.
            
            Peer peer = null;
            
            while (!_Token.IsCancellationRequested)
            {
                string peerIpPort = String.Empty;

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
                    //@TODO change this so we dont instantiate a new peer until the new peer has passed the auth challende
                    peer = new Peer(tcpPeer);

                    Log.Log.Message("*** AcceptConnections accepted connection from " + peer.Ip + peer.Port + " count " +
                        _ActivePeers);

                    Task unawaited = Task.Run(() =>
                    {
                        Task<bool> success = StartTlsStream(peer, 1);
                        if (success.Result)
                        {
                            if (!AddPeer(peer))
                            {
                                Log.Log.Message("*** FinalizeConnection unable to add peer " + peer.Ip + peer.Port);
                                return;
                            }
                        }
                    }, _Token);
                }
                catch (ObjectDisposedException ex)
                {
                    // Listener stopped ? if so, peerIpPort will be empty
                    Log.Log.Message("*** AcceptConnections ObjectDisposedException from " + peerIpPort + Environment.NewLine +
                        ex.ToString());
                }
                catch (SocketException ex)
                {
                    switch (ex.Message)
                    {
                        case "An existing connection was forcibly closed by the remote host":
                            Log.Log.Message("*** AcceptConnections SocketException " + peerIpPort + " closed the connection.");
                            break;
                        default:
                            Log.Log.Message("*** AcceptConnections SocketException from " + peerIpPort + Environment.NewLine +
                                ex.ToString());
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Log.Message("*** AcceptConnections Exception from " + peerIpPort + Environment.NewLine + ex.ToString());
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
            Peer peer = null;
            
            try
            {
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    tcpClient.Close();
                    throw new TimeoutException("Timeout connecting to " + ip + ":" + port);
                }

                tcpClient.EndConnect(ar);

                peer = new Peer(tcpClient);

                Task<bool> success = StartTlsStream(peer, 2);
                if (success.Result)
                {
                    if (!AddPeer(peer))
                    {
                        Log.Log.Message("*** FinalizeConnection unable to add peer " + peer.Ip + peer.Port);
                        return;
                    }
                }
                Task.Run(async () => await DataReceiver(peer, _Token), _Token);
            }
            
            finally
            {
                Console.WriteLine("Starting Challenge Request");

                PeerProtocol.Types.ChallengeRequest requestMessage = MessageFactory.Get(2);

                SecureRandom random = new SecureRandom();
                byte[] keyBytes = new byte[16];
                random.NextBytes(keyBytes);
                requestMessage.Nonce = random.NextInt();
                peer.nonce = requestMessage.Nonce;
                byte[] requestBytes = requestMessage.ToByteArray();
                Console.WriteLine(requestMessage);
                Console.WriteLine(HexByteConvertorExtensions.ToHex(requestBytes));             
                await Stream.Writer.MessageWriteAsync(peer,requestBytes, 98, _SendLock);
                wh.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        private bool IsConnected(Peer peer)
        {
            if (peer.TcpClient.Connected)
            {
                if ((peer.TcpClient.Client.Poll(0, SelectMode.SelectWrite)) && (!peer.TcpClient.Client.Poll(0, SelectMode.SelectError)))
                {
                    byte[] buffer = new byte[1];
                    if (peer.TcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
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
        /// disposes a peer object.
        /// </summary>
        /// <param name="ipPort"></param>
        public bool DisconnectPeer(string ip, int port)
        {
            if (!_Peers.TryGetValue(ip+":"+port, out Peer peer))
            {
                Log.Log.Message("*** DisconnectPeer unable to find peer " + peer.Ip+":"+peer.Port);
                return false;
            }
  
            
            if (!_Peers.TryRemove(peer.Ip+":"+peer.Port, out Peer removedPeer))
            {
                Log.Log.Message("*** RemovePeer unable to remove peer " + peer.Ip + peer.Port);
                return false;
            }

            peer.Dispose();
            Log.Log.Message("*** RemovePeer removed peer " + peer.Ip + peer.Port);
            return true;
        }
        
        /// <summary>
        /// returns a list of our peers
        /// </summary>
        /// <returns></returns>
        public List<string> ListPeers()
        {
            Dictionary<string, Peer> peers = _Peers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            List<string> ret = new List<string>();
            foreach (KeyValuePair<string, Peer> curr in peers)
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
            return (_Peers.TryGetValue(ip+":"+port, out Peer peer));
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        private bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // return true; // Allow untrusted certificates.
            return AcceptInvalidCerts;
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

                if (_Peers != null && _Peers.Count > 0)
                {
                    foreach (KeyValuePair<string, Peer> peer in _Peers)
                    {
                        peer.Value.Dispose();
                    }
                }
            }
            Disposed = true;            
        }
    }
}
