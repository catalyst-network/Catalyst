using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using ADL.Node.Core.Modules.Peer.IO;

namespace ADL.Node.Core.Modules.Peer
{
    /// <summary>
    /// 
    /// </summary>
    public class Network : ServerBase, IDisposable
    {
        private string _ListenerIp;
        private int _ListenerPort;
        private IPAddress _ListenerIpAddress;
        private TcpListener _Listener;
        private X509Certificate2 _SslCertificate;
        private bool _MutuallyAuthenticate;
        internal int _ActiveClients;
        private List<string> _PermittedIps;
        private static Network _instance;
        private static readonly object Mutex = new object();

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
                        _instance = new Network(
                            peerSettings,
                            sslSettings,
                            dataDir,
                            true,
                            true,
                            null,
                            PeerConnected,
                            PeerDisconnected,
                            MessageReceived,
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
            //@TODO maybe we need to extend this to stop it listening on privillaged ports up to 1023
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

            if (String.IsNullOrEmpty(peerSettings.BindAddress))
            {
                _ListenerIpAddress = IPAddress.Any;
                _ListenerIp = _ListenerIpAddress.ToString();
            }
            else
            {
                _ListenerIpAddress = IPAddress.Parse(peerSettings.BindAddress);
                _ListenerIp = peerSettings.BindAddress;
            }

            _ListenerPort = peerSettings.Port;

            _SslCertificate = null;
            if (String.IsNullOrEmpty(sslSettings.SslCertPassword))
            {
                _SslCertificate = new X509Certificate2(dataDir+"/"+sslSettings.PfxFileName);
            }
            else
            {
                _SslCertificate = new X509Certificate2(dataDir+"/"+sslSettings.PfxFileName, sslSettings.SslCertPassword);
            }

            Log("Peer server starting on " + _ListenerIp + ":" + _ListenerPort);

            _Listener = new TcpListener(_ListenerIpAddress, _ListenerPort);
            _TokenSource = new CancellationTokenSource();
            _Token = _TokenSource.Token;
            _ActiveClients = 0;
            _Clients = new ConcurrentDictionary<string, Peer>();

            // start our incoming connection data receiver thread.
            Task.Run(() => AcceptConnections(), _Token);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task AcceptConnections()
        {
            _Listener.Start();
            while (!_Token.IsCancellationRequested)
            {
                string clientIpPort = String.Empty;

                try
                {
                    TcpClient tcpClient = await _Listener.AcceptTcpClientAsync();
                    tcpClient.LingerState.Enabled = false;

                    string clientIp = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();

                    if (_PermittedIps != null && _PermittedIps.Count > 0)
                    {
                        if (!_PermittedIps.Contains(clientIp))
                        {
                            Log("*** AcceptConnections rejecting connection from " + clientIp + " (not permitted)");
                            tcpClient.Close();
                            continue;
                        }
                    }
                    
                    // inbound peer
                    //do we want to elevate a new connection as peer immediatly?
                    //@TODO change this so we dont instantiate a new peer until the new client has passed the auth challende
                    Peer client = new Peer(tcpClient);

                    Log("*** AcceptConnections accepted connection from " + client.Ip + client.Port + " count " + _ActiveClients);

                    if (AcceptInvalidCerts)
                    {
                        // accept invalid certs
                        client.SslStream = new SslStream(client.NetworkStream, false, new RemoteCertificateValidationCallback(AcceptCertificate));
                    }
                    else
                    {
                        // do not accept invalid SSL certificates
                        client.SslStream = new SslStream(client.NetworkStream, false);
                    }

                    Task unawaited = Task.Run(() => {
                        Task<bool> success = StartTls(client);
                        if (success.Result)
                        {
                            if (!AddClient(client))
                            {
                                Log("*** FinalizeConnection unable to add client " + client.Ip + client.Port);
                                client.Dispose();
                                return;
                            }

                            // Do not decrement in this block, decrement is done by the connection reader
                            int activeCount = Interlocked.Increment(ref _ActiveClients);

                            Log("*** FinalizeConnection starting data receiver for " + client.Ip + client.Port + " (now " + activeCount + " clients)");
                            if (_PeerConnected != null)
                            {
                                Task.Run(() => _PeerConnected(client.Ip, client.Port));
                            }

                            Task.Run(async () => await ConnectionWorker(client));

                            var challengeRequest = new ADL.Protocol.Peer.ChallengeRequest();
                            
                            SecureRandom random = new SecureRandom();
                            byte[] keyBytes = new byte[16];
                            random.NextBytes(keyBytes);
                            challengeRequest.Nonce = random.NextInt();
                            challengeRequest.Type = 10;
                            client.nonce = challengeRequest.Nonce;
                            Task.Run(async () => await SendAsync(client.Ip, client.Port, challengeRequest.ToByteArray()));
                        }
                    }, _Token);
                }
                catch (ObjectDisposedException ex)
                {
                    // Listener stopped ? if so, clientIpPort will be empty
                    Log("*** AcceptConnections ObjectDisposedException from " + clientIpPort + Environment.NewLine + ex.ToString());
                }
                catch (SocketException ex)
                {
                    switch (ex.Message)
                    {
                        case "An existing connection was forcibly closed by the remote host":
                            Log("*** AcceptConnections SocketException " + clientIpPort + " closed the connection.");
                            break;
                        default:
                            Log("*** AcceptConnections SocketException from " + clientIpPort + Environment.NewLine + ex.ToString());
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log("*** AcceptConnections Exception from " + clientIpPort + Environment.NewLine + ex.ToString());
                }
            }
        }
        
        //@TODO change this so we dont need to pass peer but only the tcp.SslStream
        // We shouldnt know they are a Peer until after Tls has been established
        private async Task<bool> StartTls(Peer client)
        {
            try
            {
                // the two bools in this should really be contruction paramaters
                await client.SslStream.AuthenticateAsServerAsync(_SslCertificate, true, SslProtocols.Tls12, false);

                if (!client.SslStream.IsEncrypted)
                {
                    Log("*** StartTls stream from " + client.Ip + client.Port + " not encrypted");
                    client.Dispose();
                    return false;
                }

                if (!client.SslStream.IsAuthenticated)
                {
                    Log("*** StartTls stream from " + client.Ip + client.Port + " not authenticated");
                    client.Dispose();
                    return false;
                }

                if (_MutuallyAuthenticate && !client.SslStream.IsMutuallyAuthenticated)
                {
                    Log("*** StartTls stream from " + client.Ip + client.Port + " failed mutual authentication");
                    client.Dispose();
                    return false;
                }
            }
            catch (IOException ex)
            {
                // Some type of problem initiating the SSL connection
                switch (ex.Message)
                {
                    case "Authentication failed because the remote party has closed the transport stream.":
                    case "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.":
                        Log("*** StartTls IOException " + client.Ip + client.Port + " closed the connection.");
                        break;
                    case "The handshake failed due to an unexpected packet format.":
                        Log("*** StartTls IOException " + client.Ip + client.Port + " disconnected, invalid handshake.");
                        break;
                    default:
                        Log("*** StartTls IOException from " + client.Ip + client.Port + Environment.NewLine + ex.ToString());
                        break;
                }

                client.Dispose();
                return false;
            }
            catch (Exception ex)
            {
                Log("*** StartTls Exception from " + client.Ip + client.Port + Environment.NewLine + ex.ToString());
                client.Dispose();
                return false;
            }

            return true;
        }
        
        private bool AddClient(Peer client)
        {
            if (!_Clients.TryRemove(client.Ip+":"+client.Port, out Peer removedClient))
            {
                // do nothing, it probably did not exist anyway
            }

            _Clients.TryAdd(client.Ip+":"+client.Port ,client);
            Log("*** AddClient added client " + client.Ip + client.Port);
            return true;
        }
        
        private bool RemoveClient(Peer client)
        {
            if (!_Clients.TryRemove(client.Ip+":"+client.Port, out Peer removedClient))
            {
                Log("*** RemoveClient unable to remove client " + client.Ip + client.Port);
                return false;
            }
            else
            {
                Log("*** RemoveClient removed client " + client.Ip + client.Port);
                return true;
            }
        }

        private async Task ConnectionWorker(Peer client)
        {
            try
            {

                while (true)
                {
                    try
                    {
                        if (!IsConnected(client))
                        {
                            break;
                        }

                        byte[] data = await MessageReadAsync(client);
                        if (data == null)
                        {
                            await Task.Delay(30);
                            continue;
                        }

                        if (_MessageReceived != null)
                        {
                            Task<bool> unawaited = Task.Run(() => _MessageReceived(client.Ip, client.Port, data));
                        }
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
            finally
            {
                int activeCount = Interlocked.Decrement(ref _ActiveClients);
                RemoveClient(client);
                if (_PeerDisconnected != null)
                {
                    Task<bool> unawaited = Task.Run(() => _PeerDisconnected(client.Ip, client.Port));
                }
                Log("*** DataReceiver client " + client.Ip + client.Port + " disconnected (now " + activeCount + " clients active)");

                client.Dispose();
            }
        }
        
        private bool IsConnected(Peer client)
        {
            if (client.TcpClient.Connected)
            {
                if ((client.TcpClient.Client.Poll(0, SelectMode.SelectWrite)) && (!client.TcpClient.Client.Poll(0, SelectMode.SelectError)))
                {
                    byte[] buffer = new byte[1];
                    if (client.TcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
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
                
        public void DisconnectClient(string ipPort)
        {
            if (!_Clients.TryGetValue(ipPort, out Peer client))
            {
                Log("*** DisconnectClient unable to find client " + ipPort);
            }
            else
            {
                client.Dispose();
            }
        }
        
        public List<string> ListClients()
        {
            Dictionary<string, Peer> clients = _Clients.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            List<string> ret = new List<string>();
            foreach (KeyValuePair<string, Peer> curr in clients)
            {
                ret.Add(curr.Key);
            }
            return ret;
        }
        
        public bool IsClientConnected(string ip, int port)
        {
            return (_Clients.TryGetValue(ip+":"+port, out Peer client));
        }
        
        /// <summary>
        /// dispose server and background workers.
        /// </summary>
        public override void Dispose()
        {
            if (Disposed)
            {
                return;
            }
            else
            {
                _TokenSource.Cancel();
                _TokenSource.Dispose();

                if (_Listener != null && _Listener.Server != null)
                {
                    _Listener.Server.Close();
                    _Listener.Server.Dispose();
                }

                if (_Clients != null && _Clients.Count > 0)
                {
                    foreach (KeyValuePair<string, Peer> currMetadata in _Clients)
                    {
                        currMetadata.Value.Dispose();
                    }
                }
                Disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

}