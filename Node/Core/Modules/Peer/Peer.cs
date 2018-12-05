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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ADL.Cryptography;
using Google.Protobuf;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Asn1.Mozilla;
using Org.BouncyCastle.Security;

namespace ADL.Node.Core.Modules.Peer
{
    public class Client : TcpBase, IDisposable
    {
        private bool _Disposed = false;
        private string _SourceIp;//should be same as bind ip peer setting
        private int _SourcePort;//should be same as bind port peer setting
        private string Ip;
        private int Port;
        private bool _Debug;
        private TcpClient _Tcp;
        private SslStream _Ssl;
        private X509Certificate2 _SslCertificate;
        private X509Certificate2Collection _SslCertificateCollection;
        private bool _Connected;
        private Func<byte[], bool> _MessageReceived = null;
        private Func<bool> _ServerConnected = null;
        private Func<bool> _ServerDisconnected = null;
        private readonly SemaphoreSlim _SendLock;
        
        public Client (
            string ip,
            int port,
            string pfxCertFile,
            string pfxCertPass,
            bool acceptInvalidCerts,
            bool mutualAuthentication,
            Func<bool> serverConnected,
            Func<bool> serverDisconnected,
            Func<byte[], bool> messageReceived,
            bool debug)
        {
            if (String.IsNullOrEmpty(ip))
            {
                throw new ArgumentNullException(nameof(ip));
            }

            if (port < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            Ip = ip;
            Port = port;
            AcceptInvalidCerts = acceptInvalidCerts;

            _ServerConnected = serverConnected;
            _ServerDisconnected = serverDisconnected;
            _MessageReceived = messageReceived ?? throw new ArgumentNullException(nameof(messageReceived));

            _Debug = debug;

            _SendLock = new SemaphoreSlim(1);

            _SslCertificate = null;
            if (String.IsNullOrEmpty(pfxCertPass))
            {
                _SslCertificate = new X509Certificate2(pfxCertFile);
            }
            else
            {
                _SslCertificate = new X509Certificate2(pfxCertFile, pfxCertPass);
            }

            _SslCertificateCollection = new X509Certificate2Collection
            {
                _SslCertificate
            };

            _Tcp = new TcpClient();
            IAsyncResult ar = _Tcp.BeginConnect(Ip, Port, null, null);
            WaitHandle wh = ar.AsyncWaitHandle;

            try
            {
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    _Tcp.Close();
                    throw new TimeoutException("Timeout connecting to " + Ip + ":" + Port);
                }

                _Tcp.EndConnect(ar);

                _SourceIp = ((IPEndPoint)_Tcp.Client.LocalEndPoint).Address.ToString();
                _SourcePort = ((IPEndPoint)_Tcp.Client.LocalEndPoint).Port;

                if (AcceptInvalidCerts)
                {
                    // accept invalid certs
                    _Ssl = new SslStream(_Tcp.GetStream(), false, new RemoteCertificateValidationCallback(AcceptCertificate));
                }
                else
                {
                    // do not accept invalid SSL certificates
                    _Ssl = new SslStream(_Tcp.GetStream(), false);
                }

                _Ssl.AuthenticateAsClient(Ip, _SslCertificateCollection, SslProtocols.Tls12, !AcceptInvalidCerts);

                if (!_Ssl.IsEncrypted)
                {
                    throw new AuthenticationException("Stream is not encrypted");
                }

                if (!_Ssl.IsAuthenticated)
                {
                    throw new AuthenticationException("Stream is not authenticated");
                }

                if (mutualAuthentication && !_Ssl.IsMutuallyAuthenticated)
                {
                    throw new AuthenticationException("Mutual authentication failed");
                }

                _Connected = true;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                wh.Close();
            }

            if (_ServerConnected != null)
            {
                Task.Run(() => _ServerConnected());
            }

            _TokenSource = new CancellationTokenSource();
            _Token = _TokenSource.Token;
            Task.Run(async () => await DataReceiver(_Token), _Token);
        }
        
        private async Task DataReceiver(CancellationToken? cancelToken=null)
        {
            try
            {
                while (true)
                {
                    cancelToken?.ThrowIfCancellationRequested();

                    if (_Tcp == null)
                    {
                        Log("*** DataReceiver null TCP interface detected, disconnection or close assumed");
                        break;
                    }

                    if (!_Tcp.Connected)
                    {
                        Log("*** DataReceiver server " + Ip + ":" + Port + " disconnected");
                        break;
                    }

                    byte[] data = await MessageReadAsync();
                    if (data == null)
                    {
                        await Task.Delay(30);
                        continue;
                    }

                    Task<bool> unawaited = Task.Run(() => _MessageReceived(data));
                }
            }
            catch (OperationCanceledException)
            {
                throw; //normal cancellation
            }
            catch (Exception)
            {
                Log("*** DataReceiver server " + Ip + ":" + Port + " disconnected");
            }
            finally
            {
                _Connected = false;
                _ServerDisconnected?.Invoke();
            }
        }
        
        private async Task<byte[]> MessageReadAsync()
        {
            try
            {
                if (_Tcp == null)
                {
                    Log("*** MessageReadAsync null client supplied");
                    return null;
                }

                if (!_Tcp.Connected)
                {
                    Log("*** MessageReadAsync supplied client is not connected");
                    return null;
                }

                if (_Ssl == null)
                {
                    Log("*** MessageReadAsync null SSL stream");
                    return null;
                }

                if (!_Ssl.CanRead)
                {
                    Log("*** MessageReadAsync SSL stream is unreadable");
                    return null;
                }

                int bytesRead = 0;
                int sleepInterval = 25;
                int maxTimeout = 500;
                int currentTimeout = 0;
                bool timeout = false;

                byte[] headerBytes;
                string header = "";
                long contentLength;
                byte[] contentBytes;

                using (MemoryStream headerMs = new MemoryStream())
                {
                    byte[] headerBuffer = new byte[1];
                    timeout = false;
                    currentTimeout = 0;
                    int read = 0;

                    while ((read = await _Ssl.ReadAsync(headerBuffer, 0, headerBuffer.Length)) > 0)
                    {
                        if (read > 0)
                        {
                            await headerMs.WriteAsync(headerBuffer, 0, read);
                            bytesRead += read;
                            currentTimeout = 0;

                            if (bytesRead > 1)
                            {
                                // check if end of headers reached
                                if (headerBuffer[0] == 58)
                                {
                                    break;
                                }
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
                        Log("*** MessageReadAsync malformed message from server (message header not an integer)");
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

                    while ((read = await _Ssl.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        if (read > 0)
                        {
                            await dataMs.WriteAsync(buffer, 0, read);
                            bytesRead = bytesRead + read;
                            bytesRemaining = bytesRemaining - read;

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
                    Log("*** MessageRead no content read");
                    return null;
                }

                if (contentBytes.Length != contentLength)
                {
                    Log("*** MessageRead content length " + contentBytes.Length + " bytes does not match header value of " + contentLength);
                    return null;
                }

                return contentBytes;
            }
            catch (Exception)
            {
                Log("*** MessageRead server disconnected");
                return null;
            }
        }
        
        private async Task<bool> MessageWriteAsync(byte[] data)
        {
            bool disconnectDetected = false;

            try
            {
                if (_Tcp == null)
                {
                    Log("MessageWriteAsync client is null");
                    disconnectDetected = true;
                    return false;
                }

                if (_Ssl == null)
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

                await _SendLock.WaitAsync();
                try
                {
                    _Ssl.Write(message, 0, message.Length);
                    _Ssl.Flush();
                }
                finally
                {
                    _SendLock.Release();
                }

                return true;
            }
            catch (ObjectDisposedException ObjDispInner)
            {
                Log("*** MessageWriteAsync server disconnected (obj disposed exception): " + ObjDispInner.Message);
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
                    _Connected = false;
                    Dispose();
                }
            }
        }

        public async Task<bool> SendAsync(byte[] data)
        {
            return await MessageWriteAsync(data);
        }
        
        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }
            else
            {
                if (_Tcp != null)
                {
                    if (_Tcp.Connected)
                    {
                        NetworkStream ns = _Tcp.GetStream();
                        if (ns != null)
                        {
                            ns.Close();
                        }
                    }

                    _Tcp.Close();
                }

                _Ssl.Dispose();

                _TokenSource.Cancel();
                _TokenSource.Dispose();

                _SendLock.Dispose();

                _Connected = false;   
            }
            _Disposed = true;
        }
    }

    internal class Network : TcpBase, IDisposable
    {
        private string _ListenerIp;
        private int _ListenerPort;
        private IPAddress _ListenerIpAddress;
        private TcpListener _Listener;
        private X509Certificate2 _SslCertificate;
        private bool _MutuallyAuthenticate;
        internal int _ActiveClients;
        private ConcurrentDictionary<string, Peer> _Clients;
        private List<string> _PermittedIps;
        private Func<string, int, bool> _ClientConnected = null;
        private Func<string, int, bool> _ClientDisconnected = null;
        private Func<Peer, byte[], bool> _MessageReceived = null;
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
                        _instance = new Network(
                            peerSettings,
                            sslSettings,
                            dataDir,
                            true,
                            true,
                            null,
                            ClientConnected,
                            ClientDisconnected,
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
        /// <param name="clientConnected"></param>
        /// <param name="clientDisconnected"></param>
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
            Func<string, int, bool> clientConnected,
            Func<string, int, bool> clientDisconnected,
            Func<Peer, byte[], bool> messageReceived,
            bool debug)
        {
            if (peerSettings.Port < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(peerSettings.Port));
            }

            AcceptInvalidCerts = acceptInvalidCerts;
            _MutuallyAuthenticate = mutualAuthentication;

            _ClientConnected = clientConnected;
            _ClientDisconnected = clientDisconnected;
            _MessageReceived = messageReceived ?? throw new ArgumentNullException(nameof(_MessageReceived));

            _debug = debug;

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
                _SslCertificate = new X509Certificate2(sslSettings.PfxFileName);
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

            Task.Run(() => AcceptConnections(), _Token);
        }
        
        static bool ClientConnected(string ip, int port)
        {
            Console.WriteLine("Client connected: "+ip+":"+port);
            return true;
        }

        static bool ClientDisconnected(string ip, int port)
        {
            Console.WriteLine("Client disconnected: " + ip+":"+port);
            return true;
        }

        static bool MessageReceived(Peer client, byte[] data)
        {
            Console.WriteLine("lgdflal");

            string msg = "";
            if (data != null && data.Length > 0)
            {
                msg = Encoding.UTF8.GetString(data);
            }
            Console.WriteLine("dfds");

            var charResponse = ADL.Protocol.Peer.ChallengeResponse.Parser.ParseFrom(data);
            Console.WriteLine("dfds");

            var keyFactory = PrivateKeyFactory.CreateKey(System.Convert.FromBase64String(charResponse.PublicKey));
            Console.WriteLine("llal");
            Console.WriteLine(Ec.VerifySignature(keyFactory,charResponse.SignedNonce,client.nonce.ToString()));

            Console.WriteLine("Message received from " + client.ipPort + ": " + ADL.Protocol.Peer.ChallengeResponse.Parser.ParseFrom(data));
            return true;
        }
        
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
                    Peer client = Peer.GetInstance(tcpClient);

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
                            if (_ClientConnected != null)
                            {
                                Task.Run(() => _ClientConnected(client.Ip, client.Port));
                            }

                            Task.Run(async () => await DataReceiver(client));

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
        
        private async Task DataReceiver(Peer client)
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
                            // no message available
                            await Task.Delay(30);
                            continue;
                        }

                        if (_MessageReceived != null)
                        {
                            Task<bool> unawaited = Task.Run(() => _MessageReceived(client, data));
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
                if (_ClientDisconnected != null)
                {
                    Task<bool> unawaited = Task.Run(() => _ClientDisconnected(client.Ip, client.Port));
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
        
        private async Task<byte[]> MessageReadAsync(Peer client)
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

            if (!client.SslStream.CanRead)
            {
                return null;
            }

            using (MemoryStream headerMs = new MemoryStream())
            {
                byte[] headerBuffer = new byte[1];
                timeout = false;
                currentTimeout = 0;
                int read = 0;

                while ((read = await client.SslStream.ReadAsync(headerBuffer, 0, headerBuffer.Length)) > 0)
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
                    Log("*** MessageReadAsync malformed message from " + client.Ip + client.Port + " (message header not an integer)");
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

                while ((read = await client.SslStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
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
                Log("*** MessageReadAsync " + client.Ip + client.Port + " no content read");
                return null;
            }

            if (contentBytes.Length != contentLength)
            {
                Log("*** MessageReadAsync " + client.Ip + client.Port + " content length " + contentBytes.Length + " bytes does not match header value " + contentLength + ", discarding");
                return null;
            }
            return contentBytes;
        }
        
        public async Task<bool> SendAsync(string ip, int port, byte[] data)
        {
            if (!_Clients.TryGetValue(ip+":"+port, out Peer client))
            {
                Log("*** SendAsync unable to find client " + ip+":"+port);
                return false;
            }

            return await MessageWriteAsync(client, data);
        }
        
        private async Task<bool> MessageWriteAsync(Peer client, byte[] data)
        {
            try
            {
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

                await client.SslStream.WriteAsync(message, 0, message.Length);
                await client.SslStream.FlushAsync();
                return true;
            }
            catch (Exception)
            {
                Log("*** MessageWriteAsync " + client.Ip+":"+client.Port + " disconnected due to exception");
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
        public void Dispose()
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

    /// <summary>
    /// 
    /// </summary>
    public class TcpBase
    {
        internal bool _debug = false;
        internal bool Disposed = false;
        internal bool AcceptInvalidCerts;
        internal CancellationTokenSource _TokenSource;
        internal CancellationToken _Token;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        internal bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
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
    }
    
    /// <summary>
    /// 
    /// </summary>
    public class Peer : IDisposable
    {
        public int nonce = 0;
        public string ipPort;
        public int Port;
        public string Ip;
        internal TcpClient TcpClient;
        internal SslStream SslStream;
        private bool disposed = false;
        private static Peer _instance;
        internal NetworkStream NetworkStream;
        private static readonly object Mutex = new object();

        /// <summary>
        ///
        /// </summary>
        /// <param name="tcp"></param>
        /// <returns></returns>
        public static Peer GetInstance(TcpClient tcp)
        {
            if (_instance == null)
            {
                lock (Mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new Peer(tcp);
                    }
                }
            }
            return _instance;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcp"></param>
        public Peer(TcpClient tcp)
        {
            TcpClient = tcp ?? throw new ArgumentNullException(nameof(tcp));
            NetworkStream = tcp.GetStream();
            Port = ((IPEndPoint)tcp.Client.RemoteEndPoint).Port;
            Ip = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                if (SslStream != null)
                {
                    SslStream.Close();
                }

                if (NetworkStream != null)
                {
                    NetworkStream.Close();
                }

                if (TcpClient != null)
                {
                    TcpClient.Close();
                }
            }

            disposed = true;
        }
    }
}
