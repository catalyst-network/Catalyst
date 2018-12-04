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
using WatsonTcp;

namespace ADL.Node.Core.Modules.Peer
{
    public class Network
    {
        private static Network _instance;
        private ISslSettings SslSettings { get; set; }
        private IPeerSettings PeerSettings { get; set; }
        private static readonly object Mutex = new object();

        public static Network GetInstance(IPeerSettings peerSettings, ISslSettings sslSettings, string dataDir)
        {
            if (_instance == null)
            {
                lock (Mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new Network(peerSettings, sslSettings, dataDir);
                    }
                }
            }

            return _instance;
        }

        public Network(IPeerSettings peerSettings, ISslSettings sslSettings, string dataDir)
        {
            SslSettings = sslSettings;
            PeerSettings = peerSettings;

           var server = new Server(
                PeerSettings.BindAddress,
                PeerSettings.Port,
                dataDir+"/"+SslSettings.PfxFileName,
                SslSettings.SslCertPassword,
                true,
                true,
                null,
                ClientConnected,
                ClientDisconnected,
                MessageReceived,
                true
            );
        }
        
        static bool ClientConnected(string ipPort)
        {
            Console.WriteLine("Client connected: " + ipPort);
            return true;
        }

        static bool ClientDisconnected(string ipPort)
        {
            Console.WriteLine("Client disconnected: " + ipPort);
            return true;
        }

        static bool MessageReceived(string ipPort, byte[] data)
        {
            string msg = "";
            if (data != null && data.Length > 0)
            {
                msg = Encoding.UTF8.GetString(data);
            }

            Console.WriteLine("Message received from " + ipPort + ": " + msg);
            return true;
        }
    }

    internal class Peer
    {
        
    }

    public class Client : TcpBase, IDisposable
    {
        private bool _Disposed = false;
        private string _SourceIp;
        private int _SourcePort;
        private string _ServerIp;
        private int _ServerPort;
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
        private CancellationTokenSource _TokenSource;
        private CancellationToken _Token;
        
        public Client (
            string serverIp,
            int serverPort,
            string pfxCertFile,
            string pfxCertPass,
            bool acceptInvalidCerts,
            bool mutualAuthentication,
            Func<bool> serverConnected,
            Func<bool> serverDisconnected,
            Func<byte[], bool> messageReceived,
            bool debug)
        {
            if (String.IsNullOrEmpty(serverIp))
            {
                throw new ArgumentNullException(nameof(serverIp));
            }

            if (serverPort < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(serverPort));
            }

            _ServerIp = serverIp;
            _ServerPort = serverPort;
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
            IAsyncResult ar = _Tcp.BeginConnect(_ServerIp, _ServerPort, null, null);
            WaitHandle wh = ar.AsyncWaitHandle;

            try
            {
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    _Tcp.Close();
                    throw new TimeoutException("Timeout connecting to " + _ServerIp + ":" + _ServerPort);
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

                _Ssl.AuthenticateAsClient(_ServerIp, _SslCertificateCollection, SslProtocols.Tls12, !AcceptInvalidCerts);

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
                        Log("*** DataReceiver server " + _ServerIp + ":" + _ServerPort + " disconnected");
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
                Log("*** DataReceiver server " + _ServerIp + ":" + _ServerPort + " disconnected");
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

    internal class Server : TcpBase, IDisposable
    {
        private string _ListenerIp;
        private int _ListenerPort;
        private IPAddress _ListenerIpAddress;
        private TcpListener _Listener;
        private X509Certificate2 _SslCertificate;
        private bool _MutuallyAuthenticate;
        private int _ActiveClients;
        private ConcurrentDictionary<string, ClientMetadata> _Clients;
        private List<string> _PermittedIps;
        private CancellationTokenSource _TokenSource;
        private CancellationToken _Token;
        private Func<string, bool> _ClientConnected = null;
        private Func<string, bool> _ClientDisconnected = null;
        private Func<string, byte[], bool> _MessageReceived = null;
        
        public Server(
            string listenerIp,
            int listenerPort,
            string pfxCertFile,
            string pfxCertPass,
            bool acceptInvalidCerts,
            bool mutualAuthentication,
            IEnumerable<string> permittedIps,
            Func<string, bool> clientConnected,
            Func<string, bool> clientDisconnected,
            Func<string, byte[], bool> messageReceived,
            bool debug)
        {
            if (listenerPort < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(listenerPort));
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

            if (String.IsNullOrEmpty(listenerIp))
            {
                _ListenerIpAddress = IPAddress.Any;
                _ListenerIp = _ListenerIpAddress.ToString();
            }
            else
            {
                _ListenerIpAddress = IPAddress.Parse(listenerIp);
                _ListenerIp = listenerIp;
            }

            _ListenerPort = listenerPort;

            _SslCertificate = null;
            if (String.IsNullOrEmpty(pfxCertPass))
            {
                _SslCertificate = new X509Certificate2(pfxCertFile);
            }
            else
            {
                _SslCertificate = new X509Certificate2(pfxCertFile, pfxCertPass);
            }

            Log("Peer server starting on " + _ListenerIp + ":" + _ListenerPort);

            _Listener = new TcpListener(_ListenerIpAddress, _ListenerPort);
            _TokenSource = new CancellationTokenSource();
            _Token = _TokenSource.Token;
            _ActiveClients = 0;
            _Clients = new ConcurrentDictionary<string, ClientMetadata>();

            Task.Run(() => AcceptConnections(), _Token);
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

                    ClientMetadata client = new ClientMetadata(tcpClient);
                    clientIpPort = client.IpPort;

                    Log("*** AcceptConnections accepted connection from " + client.IpPort);

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
                                Log("*** FinalizeConnection unable to add client " + client.IpPort);
                                client.Dispose();
                                return;
                            }

                            // Do not decrement in this block, decrement is done by the connection reader
                            int activeCount = Interlocked.Increment(ref _ActiveClients);

                            Log("*** FinalizeConnection starting data receiver for " + client.IpPort + " (now " + activeCount + " clients)");
                            if (_ClientConnected != null)
                            {
                                Task.Run(() => _ClientConnected(client.IpPort));
                            }

                            Task.Run(async () => await DataReceiver(client));
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
        
        private async Task<bool> StartTls(ClientMetadata client)
        {
            try
            {
                // the two bools in this should really be contruction paramaters
                // maybe re-use mutualAuthentication and acceptInvalidCerts ?
                await client.SslStream.AuthenticateAsServerAsync(_SslCertificate, true, SslProtocols.Tls12, false);

                if (!client.SslStream.IsEncrypted)
                {
                    Log("*** StartTls stream from " + client.IpPort + " not encrypted");
                    client.Dispose();
                    return false;
                }

                if (!client.SslStream.IsAuthenticated)
                {
                    Log("*** StartTls stream from " + client.IpPort + " not authenticated");
                    client.Dispose();
                    return false;
                }

                if (_MutuallyAuthenticate && !client.SslStream.IsMutuallyAuthenticated)
                {
                    Log("*** StartTls stream from " + client.IpPort + " failed mutual authentication");
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
                        Log("*** StartTls IOException " + client.IpPort + " closed the connection.");
                        break;
                    case "The handshake failed due to an unexpected packet format.":
                        Log("*** StartTls IOException " + client.IpPort + " disconnected, invalid handshake.");
                        break;
                    default:
                        Log("*** StartTls IOException from " + client.IpPort + Environment.NewLine + ex.ToString());
                        break;
                }

                client.Dispose();
                return false;
            }
            catch (Exception ex)
            {
                Log("*** StartTls Exception from " + client.IpPort + Environment.NewLine + ex.ToString());
                client.Dispose();
                return false;
            }

            return true;
        }
        
        private bool AddClient(ClientMetadata client)
        {
            if (!_Clients.TryRemove(client.IpPort, out ClientMetadata removedClient))
            {
                // do nothing, it probably did not exist anyway
            }

            _Clients.TryAdd(client.IpPort, client);
            Log("*** AddClient added client " + client.IpPort);
            return true;
        }
        
        private bool RemoveClient(ClientMetadata client)
        {
            if (!_Clients.TryRemove(client.IpPort, out ClientMetadata removedClient))
            {
                Log("*** RemoveClient unable to remove client " + client.IpPort);
                return false;
            }
            else
            {
                Log("*** RemoveClient removed client " + client.IpPort);
                return true;
            }
        }
        
        private async Task DataReceiver(ClientMetadata client)
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
                            Task<bool> unawaited = Task.Run(() => _MessageReceived(client.IpPort, data));
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
                    Task<bool> unawaited = Task.Run(() => _ClientDisconnected(client.IpPort));
                }
                Log("*** DataReceiver client " + client.IpPort + " disconnected (now " + activeCount + " clients active)");

                client.Dispose();
            }
        }
        
        private bool IsConnected(ClientMetadata client)
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
        
        private async Task<byte[]> MessageReadAsync(ClientMetadata client)
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
                    Log("*** MessageReadAsync malformed message from " + client.IpPort + " (message header not an integer)");
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
                Log("*** MessageReadAsync " + client.IpPort + " no content read");
                return null;
            }

            if (contentBytes.Length != contentLength)
            {
                Log("*** MessageReadAsync " + client.IpPort + " content length " + contentBytes.Length + " bytes does not match header value " + contentLength + ", discarding");
                return null;
            }
            return contentBytes;
        }
        
        public async Task<bool> SendAsync(string ipPort, byte[] data)
        {
            if (!_Clients.TryGetValue(ipPort, out ClientMetadata client))
            {
                Log("*** SendAsync unable to find client " + ipPort);
                return false;
            }

            return await MessageWriteAsync(client, data);
        }
        
        private async Task<bool> MessageWriteAsync(ClientMetadata client, byte[] data)
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
                Log("*** MessageWriteAsync " + client.IpPort + " disconnected due to exception");
                return false;
            }
        }

        public void DisconnectClient(string ipPort)
        {
            if (!_Clients.TryGetValue(ipPort, out ClientMetadata client))
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
            Dictionary<string, ClientMetadata> clients = _Clients.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            List<string> ret = new List<string>();
            foreach (KeyValuePair<string, ClientMetadata> curr in clients)
            {
                ret.Add(curr.Key);
            }
            return ret;
        }
        
        public bool IsClientConnected(string ipPort)
        {
            return (_Clients.TryGetValue(ipPort, out ClientMetadata client));
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
                    foreach (KeyValuePair<string, ClientMetadata> currMetadata in _Clients)
                    {
                        currMetadata.Value.Dispose();
                    }
                }
                Disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    public class TcpBase
    {
        internal bool _debug = false;
        internal bool Disposed = false;
        internal bool AcceptInvalidCerts;

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
}
