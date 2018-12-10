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
using System.Text;
using ADL.Cryptography;

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

            Log("Peer server starting on " + _ListenerIpAddress + ":" + peerSettings.Port);

            _ActivePeers = 0;
            _Listener = new TcpListener(_ListenerIpAddress, peerSettings.Port);
            _TokenSource = new CancellationTokenSource();
            _Token = _TokenSource.Token;
            _Peers = new ConcurrentDictionary<string, Peer>();

            InboundConnectionListener();
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
                    Log("*** StartOutboundTls stream from " + peer.Ip + peer.Port + " not authenticated");
                    peer.Dispose();
                    throw new AuthenticationException();
                }
                if (_MutuallyAuthenticate && !peer.SslStream.IsMutuallyAuthenticated)
                {
                    Log("*** StartOutboundTls stream from " + peer.Ip + peer.Port + " failed mutual authentication");
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
                        Log("*** StartTls IOException " + peer.Ip + peer.Port + " closed the connection.");
                        break;
                    case "The handshake failed due to an unexpected packet format.":
                        Log("*** StartTls IOException " + peer.Ip + peer.Port + " disconnected, invalid handshake.");
                        break;
                    default:
                        Log("*** StartTls IOException from " + peer.Ip + peer.Port + Environment.NewLine + ex.ToString());
                        break;
                }

                peer.Dispose();
                return false;
            }
            catch (Exception ex)
            {
                Log("*** StartInboundTls Exception from " + peer.Ip + peer.Port +  Environment.NewLine + ex.ToString());
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
                Log("*** FinalizeConnection starting data receiver for " + peer.Ip + peer.Port + " (now " + activeCount + " peers)");
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
        /// <returns></returns>
        private bool RemovePeer(Peer peer)
        {
            if (!_Peers.TryRemove(peer.Ip+":"+peer.Port, out Peer removedPeer))
            {
                Log("*** RemovePeer unable to remove peer " + peer.Ip + peer.Port);
                return false;
            }
            else
            {
                Log("*** RemovePeer removed peer " + peer.Ip + peer.Port);
                return true;
            }
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
                        Log("*** DataReceiver null TCP interface detected, disconnection or close assumed");
                        break;
                    }

                    byte[] data = await MessageReadAsync(peer);
                    if (data == null)
                    {
                        await Task.Delay(30);
                        continue;
                    }
                    
                    Task<string> unawaited = Task.Run(() =>
                    {
                        var charResponse = ADL.Protocol.Peer.ChallengeResponse.Parser.ParseFrom(data);
//                        var keyFactory = PrivateKeyFactory.CreateKey(System.Convert.FromBase64String(charResponse.PublicKey));
//                        Console.WriteLine(Ec.VerifySignature(keyFactory,charResponse.SignedNonce,peer.nonce.ToString()));
                        Console.WriteLine("Message received from " + ip+":"+port + ": " + ADL.Protocol.Peer.ChallengeResponse.Parser.ParseFrom(data));
                        
                        string msg = "";
                        if (data != null && data.Length > 0)
                        {
                            msg = Encoding.UTF8.GetString(data);
                        }
                        Console.WriteLine(msg);
                        return msg;
                    });
                }
            }
            catch (OperationCanceledException)
            {
                throw; //normal cancellation
            }
            catch (Exception e)
            {
                Log("*** OutboundChannelListener exception server " + ip + ":" + port + " disconnected");
                LogException("OutboundChannelListener",e);
            }
            finally
            {
                int activeCount = Interlocked.Decrement(ref _ActivePeers);
                RemovePeer(peer);
                Task<bool> unawaited = Task.Run(() => PeerDisconnected(peer.Ip, peer.Port));

                Log("***** DataReceiver peer " + peer.Ip + peer.Port + " disconnected (now " + activeCount + " peers active)");

                DisconnectPeer(peer.Ip, peer.Port);
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
                            Log("*** AcceptConnections rejecting connection from " + peerIp + " (not permitted)");
                            tcpPeer.Close();
                            continue;
                        }
                    }

                    // inbound peer
                    //do we want to elevate a new connection as peer immediatly?
                    //@TODO change this so we dont instantiate a new peer until the new peer has passed the auth challende
                    peer = new Peer(tcpPeer);

                    Log("*** AcceptConnections accepted connection from " + peer.Ip + peer.Port + " count " +
                        _ActivePeers);

                    Task unawaited = Task.Run(() =>
                    {
                        Task<bool> success = StartTlsStream(peer, 1);
                        if (success.Result)
                        {
                            if (!AddPeer(peer))
                            {
                                Log("*** FinalizeConnection unable to add peer " + peer.Ip + peer.Port);
                                return;
                            }
                        }
                    }, _Token);
                }
                catch (ObjectDisposedException ex)
                {
                    // Listener stopped ? if so, peerIpPort will be empty
                    Log("*** AcceptConnections ObjectDisposedException from " + peerIpPort + Environment.NewLine +
                        ex.ToString());
                }
                catch (SocketException ex)
                {
                    switch (ex.Message)
                    {
                        case "An existing connection was forcibly closed by the remote host":
                            Log("*** AcceptConnections SocketException " + peerIpPort + " closed the connection.");
                            break;
                        default:
                            Log("*** AcceptConnections SocketException from " + peerIpPort + Environment.NewLine +
                                ex.ToString());
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log("*** AcceptConnections Exception from " + peerIpPort + Environment.NewLine + ex.ToString());
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
                        Log("*** FinalizeConnection unable to add peer " + peer.Ip + peer.Port);
                        return;
                    }
                }
                Task.Run(async () => await DataReceiver(peer, _Token), _Token);
            }
            
            finally
            {
                Console.WriteLine("traceing");
                var challengeRequest = new ADL.Protocol.Peer.ChallengeRequest();
                SecureRandom random = new SecureRandom();
                byte[] keyBytes = new byte[16];
                random.NextBytes(keyBytes);
                challengeRequest.Nonce = random.NextInt();
                challengeRequest.Type = 10;
                peer.nonce = challengeRequest.Nonce;
                Log(challengeRequest.ToString());
                await MessageWriteAsync(peer,challengeRequest.ToByteArray());
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
        public void DisconnectPeer(string ip, int port)
        {
            if (!_Peers.TryGetValue(ip+":"+port, out Peer peer))
            {
                Log("*** DisconnectPeer unable to find peer " + peer.Ip+":"+peer.Port);
            }
            else
            {
                peer.Dispose();
                PeerDisconnected(ip, port);
            }
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
        /// <param name="peer"></param>
        /// <returns></returns>
        private async Task<byte[]> MessageReadAsync(Peer peer)
        {
            int bytesRead = 0;
            int sleepInterval = 25;
            int maxTimeout = 500;
            int currentTimeout = 0;
            bool timeout = false;

            byte[] headerBytes;
            string header = "";
            long contentLength;
            byte[] contentBytes;

            if (!peer.SslStream.CanRead)
            {
                return null;
            }

            using (MemoryStream headerMs = new MemoryStream())
            {
                byte[] headerBuffer = new byte[1];
                timeout = false;
                currentTimeout = 0;
                Int32 read = 0;

                while ((read = await peer.SslStream.ReadAsync(headerBuffer, 0, headerBuffer.Length)) > 0)
                {
                    if (read > 0)
                    {
                        await headerMs.WriteAsync(headerBuffer, 0, read);
                        bytesRead += read;

                        // reset timeout since there was a successful read
                        currentTimeout = 0;
                    }
                    else
                    {
                        if (currentTimeout >= maxTimeout)
                        {
                            timeout = true;
                            break;
                        }
                        else
                        {
                            currentTimeout += sleepInterval;
                            await Task.Delay(sleepInterval);
                        }

                        if (timeout)
                        {
                            break;
                        }
                    }

                    if (bytesRead > 1)
                    {
                        // check if end of headers reached
                        if (headerBuffer[0] == 58)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (currentTimeout >= maxTimeout)
                        {
                            timeout = true;
                            break;
                        }
                        else
                        {
                            currentTimeout += sleepInterval;
                            await Task.Delay(sleepInterval);
                        }

                        if (timeout)
                        {
                            break;
                        }
                    }
                }

                if (timeout)
                {
                    Log("*** MessageReadAsync timeout " + currentTimeout + "ms/" + maxTimeout + "ms exceeded while reading header after reading " + bytesRead + " bytes");
                    return null;
                }

                headerBytes = headerMs.ToArray();
                if (headerBytes == null || headerBytes.Length < 1)
                {
                    return null;
                }

                header = Encoding.UTF8.GetString(headerBytes);
                header = header.Replace(":", "");

                if (!Int64.TryParse(header, out contentLength))
                {
                    Log("*** MessageReadAsync malformed message from " + peer.Ip + peer.Port + " (message header not an integer)");
                    return null;
                }
            }
            
            using (MemoryStream dataMs = new MemoryStream())
            {
                long bytesRemaining = contentLength;
                timeout = false;
                currentTimeout = 0;

                int read = 0;
                byte[] buffer;
                long bufferSize = 2048;
                if (bufferSize > bytesRemaining)
                {
                    bufferSize = bytesRemaining;
                }

                buffer = new byte[bufferSize];

                while ((read = await peer.SslStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    if (read > 0)
                    {
                        dataMs.Write(buffer, 0, read);
                        bytesRead = bytesRead + read;
                        bytesRemaining = bytesRemaining - read;

                        // reset timeout
                        currentTimeout = 0;

                        // reduce buffer size if number of bytes remaining is
                        // less than the pre-defined buffer size of 2KB
                        if (bytesRemaining < bufferSize)
                        {
                            bufferSize = bytesRemaining;
                        }

                        buffer = new byte[bufferSize];

                        // check if read fully
                        if (bytesRemaining == 0)
                        {
                            break;
                        }

                        if (bytesRead == contentLength)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (currentTimeout >= maxTimeout)
                        {
                            timeout = true;
                            break;
                        }
                        else
                        {
                            currentTimeout += sleepInterval;
                            await Task.Delay(sleepInterval);
                        }

                        if (timeout)
                        {
                            break;
                        }
                    }
                }

                if (timeout)
                {
                    Log("*** MessageReadAsync timeout " + currentTimeout + "ms/" + maxTimeout + "ms exceeded while reading content after reading " + bytesRead + " bytes");
                    return null;
                }

                contentBytes = dataMs.ToArray();
            }

            if (contentBytes == null || contentBytes.Length < 1)
            {
                Log("*** MessageReadAsync " + peer.Ip + peer.Port + " no content read");
                return null;
            }

            if (contentBytes.Length != contentLength)
            {
                Log("*** MessageReadAsync " + peer.Ip + peer.Port + " content length " + contentBytes.Length + " bytes does not match header value " + contentLength + ", discarding");
                return null;
            }
            Console.WriteLine("contentBytes");
            Console.WriteLine(contentBytes);
            return contentBytes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="data"></param>
        /// <param name="sendLock"></param>
        /// <returns></returns>
        private async Task<bool> MessageWriteAsync(Peer peer, byte[] data)
        {
            Console.WriteLine("Write started");
            bool disconnectDetected = false;

            try
            {
                if (peer == null)
                {
                    Log("MessageWriteAsync peer is null");
                    disconnectDetected = true;
                    return false;
                }

                if (peer.SslStream == null)
                {
                    Log("MessageWriteAsync SSL stream is null");
                    disconnectDetected = true;
                    return false;
                }

                string header = "";
                byte[] headerBytes;
                byte[] message;

                if (data == null || data.Length < 1)
                {
                    header += "0:";
                }
                else
                {
                    header += data.Length + ":";
                }

                headerBytes = Encoding.UTF8.GetBytes(header);
                int messageLen = headerBytes.Length;
                if (data != null && data.Length > 0)
                {
                    messageLen += data.Length;
                }

                message = new byte[messageLen];
                Buffer.BlockCopy(headerBytes, 0, message, 0, headerBytes.Length);

                if (data != null && data.Length > 0)
                {
                    Buffer.BlockCopy(data, 0, message, headerBytes.Length, data.Length);
                }

                // use semaphore to lock thread while we write to peer
                if (_SendLock != null)
                {
                    await _SendLock.WaitAsync();
                    try
                    {
                        peer.SslStream.Write(message, 0, message.Length);
                        peer.SslStream.Flush();
                    }
                    finally
                    {
                        _SendLock.Release();
                    }
                }
                else
                {
                    await peer.SslStream.WriteAsync(message, 0, message.Length);
                    await peer.SslStream.FlushAsync();
                }

                return true;
            }
            catch (ObjectDisposedException ObjDispInner)
            {

                Log("*** MessageWriteAsync server disconnected (obj disposed exception): " + peer.Ip + ":" + peer.Port +
                    ObjDispInner.Message);
                disconnectDetected = true;
                return false;
            }
            catch (SocketException SockInner)
            {
                Log("*** MessageWriteAsync server disconnected (socket exception): " + SockInner.Message);
                disconnectDetected = true;
                return false;
            }
            catch (InvalidOperationException InvOpInner)
            {
                Log("*** MessageWriteAsync server disconnected (invalid operation exception): " + InvOpInner.Message);
                disconnectDetected = true;
                return false;
            }
            catch (IOException IOInner)
            {
                Log("*** MessageWriteAsync server disconnected (IO exception): " + IOInner.Message);
                disconnectDetected = true;
                return false;
            }
            catch (Exception e)
            {
                LogException("MessageWriteAsync", e);
                disconnectDetected = true;
                return false;
            }
            finally
            {
                if (disconnectDetected)
                {
                    peer.Dispose();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private static bool PeerDisconnected(string ip, int port)
        {
            Console.WriteLine("Peer disconnected: " + ip+":"+port);
            return true;
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
                
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        internal void Log(string msg)
        {
            if (_debug)
            {
                Console.WriteLine(msg);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="e"></param>
        internal void LogException(string method, Exception e)
        {
            Log("================================================================================");
            Log(" = Method: " + method);
            Log(" = Exception Type: " + e.GetType().ToString());
            Log(" = Exception Data: " + e.Data);
            Log(" = Inner Exception: " + e.InnerException);
            Log(" = Exception Message: " + e.Message);
            Log(" = Exception Source: " + e.Source);
            Log(" = Exception StackTrace: " + e.StackTrace);
            Log("================================================================================");
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
