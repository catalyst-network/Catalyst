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
    }
    
    public class Peer
    {
        new WatsonTcpSslServer
    }
    
    internal class Client : TcpBase, IDisposable
    {
        private bool _Disposed = false;
        private string _SourceIp;
        private int _SourcePort;
        private string _ServerIp;
        private int _ServerPort;
        private TcpClient _Tcp;
        private SslStream _Ssl;
        private X509Certificate2 _SslCertificate;
        private X509Certificate2Collection _SslCertificateCollection;
        private bool _AcceptInvalidCerts;
        private bool _Connected;
        private Func<byte[], bool> _MessageReceived = null;
        private Func<bool> _ServerConnected = null;
        private Func<bool> _ServerDisconnected = null;

        private readonly SemaphoreSlim _SendLock;
        private CancellationTokenSource _TokenSource;
        private CancellationToken _Token;

        /// <summary>
        /// Initialize the TCP client.
        /// </summary>
        /// <param name="serverIp">The IP address or hostname of the server.</param>
        /// <param name="serverPort">The TCP port on which the server is listening.</param>
        /// <param name="pfxCertFile">The file containing the SSL certificate.</param>
        /// <param name="pfxCertPass">The password for the SSL certificate.</param>
        /// <param name="acceptInvalidCerts">True to accept invalid or expired SSL certificates.</param>
        /// <param name="mutualAuthentication">True to mutually authenticate client and server.</param>
        /// <param name="serverConnected">Function to be called when the server connects.</param>
        /// <param name="serverDisconnected">Function to be called when the connection is severed.</param>
        /// <param name="messageReceived">Function to be called when a message is received.</param>
        /// <param name="debug">Enable or debug logging messages.</param>
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
            _AcceptInvalidCerts = acceptInvalidCerts;

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

                if (_AcceptInvalidCerts)
                {
                    // accept invalid certs
                    _Ssl = new SslStream(_Tcp.GetStream(), false, new RemoteCertificateValidationCallback(AcceptCertificate));
                }
                else
                {
                    // do not accept invalid SSL certificates
                    _Ssl = new SslStream(_Tcp.GetStream(), false);
                }

                _Ssl.AuthenticateAsClient(_ServerIp, _SslCertificateCollection, SslProtocols.Tls12, !_AcceptInvalidCerts);

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

        /// <summary>
        /// Tear down the client and dispose of background workers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Determine whether or not the client is connected to the server.
        /// </summary>
        /// <returns>Boolean indicating if the client is connected to the server.</returns>
        public bool IsConnected()
        {
            return _Connected;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
            {
                return;
            }

            if (disposing)
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

        private bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // return true; // Allow untrusted certificates.
            return _AcceptInvalidCerts;
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
    }

    internal class Server : TcpBase, IDisposable
    {
        private bool _Disposed;
        private bool _Debug;
        private string _ListenerIp;
        private int _ListenerPort;
        private IPAddress _ListenerIpAddress;
        private TcpListener _Listener;
        private X509Certificate2 _SslCertificate;
        private bool _AcceptInvalidCerts;
        private bool _MutuallyAuthenticate;
        private int _ActiveClients;
        private ConcurrentDictionary<string, ClientMetadata> _Clients;
        private List<string> _PermittedIps;
        private CancellationTokenSource _TokenSource;
        private CancellationToken _Token;
        private Func<string, bool> _ClientConnected;
        private Func<string, bool> _ClientDisconnected;
        private Func<string, byte[], bool> _MessageReceived;

        public Server (
            string listenerIp,
            int listenerPort,
            string pfxCertFile,
            string pfxCertPass,
            bool acceptInvalidCerts,
            bool mutualAuthentication,
            Func<string, bool> clientConnected,
            Func<string, bool> clientDisconnected,
            Func<string, byte[], bool> messageReceived,
            bool debug)
            : this(listenerIp, listenerPort, pfxCertFile, pfxCertPass, acceptInvalidCerts, mutualAuthentication,
                (IEnumerable<string>) null, clientConnected, clientDisconnected, messageReceived, debug)
        {
        }

        public Server (
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
                throw new ArgumentOutOfRangeException(nameof(listenerPort));
            this._AcceptInvalidCerts = acceptInvalidCerts;
            this._MutuallyAuthenticate = mutualAuthentication;
            this._ClientConnected = clientConnected;
            this._ClientDisconnected = clientDisconnected;
            Func<string, byte[], bool> func = messageReceived;
            if (func == null)
                throw new ArgumentNullException(nameof(_MessageReceived));
            this._MessageReceived = func;
            this._Debug = debug;
            if (permittedIps != null && permittedIps.Count<string>() > 0)
                this._PermittedIps = new List<string>(permittedIps);
            if (string.IsNullOrEmpty(listenerIp))
            {
                this._ListenerIpAddress = IPAddress.Any;
                this._ListenerIp = this._ListenerIpAddress.ToString();
            }
            else
            {
                this._ListenerIpAddress = IPAddress.Parse(listenerIp);
                this._ListenerIp = listenerIp;
            }

            this._ListenerPort = listenerPort;
            this._SslCertificate = (X509Certificate2) null;
            this._SslCertificate = !string.IsNullOrEmpty(pfxCertPass)
                ? new X509Certificate2(pfxCertFile, pfxCertPass)
                : new X509Certificate2(pfxCertFile);
            this.Log("Server starting on " + this._ListenerIp + ":" + (object) this._ListenerPort);
            this._Listener = new TcpListener(this._ListenerIpAddress, this._ListenerPort);
            this._TokenSource = new CancellationTokenSource();
            this._Token = this._TokenSource.Token;
            this._ActiveClients = 0;
            this._Clients = new ConcurrentDictionary<string, ClientMetadata>();
            Task.Run((Func<Task>) (() => this.AcceptConnections()), this._Token);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object) this);
        }

        public bool IsClientConnected(string ipPort)
        {
            ClientMetadata clientMetadata;
            return this._Clients.TryGetValue(ipPort, out clientMetadata);
        }

        public List<string> ListClients()
        {
            Dictionary<string, ClientMetadata> dictionary =
                this._Clients.ToDictionary<KeyValuePair<string, ClientMetadata>, string, ClientMetadata>(
                    (Func<KeyValuePair<string, ClientMetadata>, string>) (kvp => kvp.Key),
                    (Func<KeyValuePair<string, ClientMetadata>, ClientMetadata>) (kvp => kvp.Value));
            List<string> stringList = new List<string>();
            foreach (KeyValuePair<string, ClientMetadata> keyValuePair in dictionary)
                stringList.Add(keyValuePair.Key);
            return stringList;
        }

        public void DisconnectClient(string ipPort)
        {
            ClientMetadata clientMetadata;
            if (!this._Clients.TryGetValue(ipPort, out clientMetadata))
                this.Log("*** DisconnectClient unable to find client " + ipPort);
            else
                clientMetadata.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this._Disposed)
                return;
            if (disposing)
            {
                this._TokenSource.Cancel();
                this._TokenSource.Dispose();
                if (this._Listener != null && this._Listener.Server != null)
                {
                    this._Listener.Server.Close();
                    this._Listener.Server.Dispose();
                }

                if (this._Clients != null && this._Clients.Count > 0)
                {
                    foreach (KeyValuePair<string, ClientMetadata> client in this._Clients)
                        client.Value.Dispose();
                }
            }

            this._Disposed = true;
        }

        private bool AcceptCertificate (
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return this._AcceptInvalidCerts;
        }

        private async Task AcceptConnections()
        {
            _Listener.Start();
            while (!Server1._Token.IsCancellationRequested)
            {
                string clientIpPort = string.Empty;
                try
                {
                    Server Server = Server1;
                    TcpClient tcp = await Server1._Listener.AcceptTcpClientAsync();
                    tcp.LingerState.Enabled = false;
                    string str = ((IPEndPoint) tcp.Client.RemoteEndPoint).Address.ToString();
                    if (Server1._PermittedIps != null && Server1._PermittedIps.Count > 0 &&
                        !Server1._PermittedIps.Contains(str))
                    {
                        Server1.Log("*** AcceptConnections rejecting connection from " + str +
                                                " (not permitted)");
                        tcp.Close();
                        continue;
                    }

                    ClientMetadata client = new ClientMetadata(tcp);
                    clientIpPort = client.IpPort;
                    Server1.Log("*** AcceptConnections accepted connection from " + client.IpPort);
                    client.SslStream = !Server1._AcceptInvalidCerts
                        ? new SslStream((Stream) client.NetworkStream, false)
                        : new SslStream((Stream) client.NetworkStream, false,
                            new RemoteCertificateValidationCallback(Server1.AcceptCertificate));
                    Task.Run((Action) (() =>
                    {
                        if (!Server.StartTls(client).Result)
                            return;
                        Server.FinalizeConnection(client);
                    }), Server1._Token);
                }
                catch (ObjectDisposedException ex)
                {
                    Server1.Log("*** AcceptConnections ObjectDisposedException from " + clientIpPort +
                                            Environment.NewLine + ex.ToString());
                }
                catch (SocketException ex)
                {
                    if (ex.Message == "An existing connection was forcibly closed by the remote host")
                        Server1.Log("*** AcceptConnections SocketException " + clientIpPort +
                                                " closed the connection.");
                    else
                        Server1.Log("*** AcceptConnections SocketException from " + clientIpPort +
                                                Environment.NewLine + ex.ToString());
                }
                catch (Exception ex)
                {
                    Server1.Log("*** AcceptConnections Exception from " + clientIpPort +
                                            Environment.NewLine + ex.ToString());
                }

                clientIpPort = (string) null;
            }
        }

        private async Task<bool> StartTls(ClientMetadata client)
        {
            try
            {
                await client.SslStream.AuthenticateAsServerAsync((X509Certificate) this._SslCertificate, true,
                    SslProtocols.Tls12, false);
                if (!client.SslStream.IsEncrypted)
                {
                    this.Log("*** StartTls stream from " + client.IpPort + " not encrypted");
                    client.Dispose();
                    return false;
                }

                if (!client.SslStream.IsAuthenticated)
                {
                    this.Log("*** StartTls stream from " + client.IpPort + " not authenticated");
                    client.Dispose();
                    return false;
                }

                if (this._MutuallyAuthenticate)
                {
                    if (!client.SslStream.IsMutuallyAuthenticated)
                    {
                        this.Log("*** StartTls stream from " + client.IpPort + " failed mutual authentication");
                        client.Dispose();
                        return false;
                    }
                }
            }
            catch (IOException ex)
            {
                string message = ex.Message;
                if (!(message == "Authentication failed because the remote party has closed the transport stream.") &&
                    !(message ==
                      "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host."
                        ))
                {
                    if (message == "The handshake failed due to an unexpected packet format.")
                        this.Log("*** StartTls IOException " + client.IpPort + " disconnected, invalid handshake.");
                    else
                        this.Log("*** StartTls IOException from " + client.IpPort + Environment.NewLine +
                                 ex.ToString());
                }
                else
                    this.Log("*** StartTls IOException " + client.IpPort + " closed the connection.");

                client.Dispose();
                return false;
            }
            catch (Exception ex)
            {
                this.Log("*** StartTls Exception from " + client.IpPort + Environment.NewLine + ex.ToString());
                client.Dispose();
                return false;
            }

            return true;
        }

        private void FinalizeConnection(ClientMetadata client)
        {
            if (!this.AddClient(client))
            {
                this.Log("*** FinalizeConnection unable to add client " + client.IpPort);
                client.Dispose();
            }
            else
            {
                int num = Interlocked.Increment(ref this._ActiveClients);
                this.Log("*** FinalizeConnection starting data receiver for " + client.IpPort + " (now " +
                         (object) num + " clients)");
                if (this._ClientConnected != null)
                    Task.Run<bool>((Func<bool>) (() => this._ClientConnected(client.IpPort)));
                Task.Run((Func<Task>) (async () => await this.DataReceiver(client)));
            }
        }

        private bool IsConnected(ClientMetadata client)
        {
            if (!client.TcpClient.Connected || !client.TcpClient.Client.Poll(0, SelectMode.SelectWrite) ||
                client.TcpClient.Client.Poll(0, SelectMode.SelectError))
                return false;
            byte[] buffer = new byte[1];
            return client.TcpClient.Client.Receive(buffer, SocketFlags.Peek) != 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
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

        private bool AddClient(ClientMetadata client)
        {
            ClientMetadata clientMetadata;
            this._Clients.TryRemove(client.IpPort, out clientMetadata);
            this._Clients.TryAdd(client.IpPort, client);
            this.Log("*** AddClient added client " + client.IpPort);
            return true;
        }
        private bool RemoveClient(ClientMetadata client)
        {
            ClientMetadata clientMetadata;
            if (!this._Clients.TryRemove(client.IpPort, out clientMetadata))
            {
                this.Log("*** RemoveClient unable to remove client " + client.IpPort);
                return false;
            }

            this.Log("*** RemoveClient removed client " + client.IpPort);
            return true;
        }
    }

    internal abstract class TcpBase
    {
        internal bool _Debug;

        public async Task<bool> SendAsync(string ipPort, byte[] data)
        {
            ClientMetadata client;
            if (this._Clients.TryGetValue(ipPort, out client))
                return await this.MessageWriteAsync(client, data);
            this.Log("*** SendAsync unable to find client " + ipPort);
            return false;
        }

        private string BytesToHex(byte[] data)
        {
            if (data == null || data.Length < 1)
                return "(null)";
            return BitConverter.ToString(data).Replace("-", "");
        }
        
        internal void Log(string msg)
        {
            if (_Debug)
            {
                Console.WriteLine(msg);
            }
        }

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
        
        internal async Task<bool> MessageWriteAsync(ClientMetadata client, byte[] data)
        {
            try
            {
                string str = "";
                byte[] bytes = Encoding.UTF8.GetBytes(data == null || data.Length < 1
                    ? str + "0:"
                    : str + (object) data.Length + ":");
                int length = bytes.Length;
                if (data != null && data.Length != 0)
                    length += data.Length;
                byte[] buffer = new byte[length];
                Buffer.BlockCopy((Array) bytes, 0, (Array) buffer, 0, bytes.Length);
                if (data != null && data.Length != 0)
                    Buffer.BlockCopy((Array) data, 0, (Array) buffer, bytes.Length, data.Length);
                await client.SslStream.WriteAsync(buffer, 0, buffer.Length);
                await client.SslStream.FlushAsync();
                return true;
            }
            catch (Exception ex)
            {
                this.Log("*** MessageWriteAsync " + client.IpPort + " disconnected due to exception");
                return false;
            }
        }
        
        internal async Task<byte[]> MessageReadAsync(ClientMetadata client)
        {
            Server Server = this;
            int bytesRead = 0;
            int sleepInterval = 25;
            int maxTimeout = 500;
            int currentTimeout = 0;
            bool timeout = false;
            if (!client.SslStream.CanRead)
                return (byte[]) null;
            long contentLength;
            using (MemoryStream headerMs = new MemoryStream())
            {
                byte[] headerBuffer = new byte[1];
                timeout = false;
                currentTimeout = 0;
                int read = 0;
                do
                {
                    do
                    {
                        int num = await client.SslStream.ReadAsync(headerBuffer, 0, headerBuffer.Length);
                        if ((read = num) > 0)
                        {
                            if (read > 0)
                            {
                                await headerMs.WriteAsync(headerBuffer, 0, read);
                                bytesRead += read;
                                currentTimeout = 0;
                            }
                            else if (currentTimeout < maxTimeout)
                            {
                                currentTimeout += sleepInterval;
                                await Task.Delay(sleepInterval);
                                if (timeout)
                                    break;
                            }
                            else
                                goto label_7;

                            if (bytesRead <= 1)
                            {
                                if (currentTimeout < maxTimeout)
                                {
                                    currentTimeout += sleepInterval;
                                    await Task.Delay(sleepInterval);
                                }
                                else
                                    goto label_13;
                            }
                            else
                                goto label_11;
                        }
                        else
                            break;
                    } while (!timeout);

                    break;
                    label_11: ;
                } while (headerBuffer[0] != (byte) 58);

                goto label_18;
                label_7:
                timeout = true;
                goto label_18;
                label_13:
                timeout = true;
                label_18:
                if (timeout)
                {
                    Server.Log("*** MessageReadAsync timeout " + (object) currentTimeout + "ms/" +
                                           (object) maxTimeout + "ms exceeded while reading header after reading " +
                                           (object) bytesRead + " bytes");
                    return (byte[]) null;
                }

                byte[] array = headerMs.ToArray();
                if (array == null || array.Length < 1)
                    return (byte[]) null;
                if (!long.TryParse(Encoding.UTF8.GetString(array).Replace(":", ""), out contentLength))
                {
                    Server.Log("*** MessageReadAsync malformed message from " + client.IpPort +
                                           " (message header not an integer)");
                    return (byte[]) null;
                }

                headerBuffer = (byte[]) null;
            }

            byte[] array1;
            using (MemoryStream dataMs = new MemoryStream())
            {
                long bytesRemaining = contentLength;
                timeout = false;
                currentTimeout = 0;
                long bufferSize = 2048;
                if (bufferSize > bytesRemaining)
                    bufferSize = bytesRemaining;
                byte[] buffer = new byte[bufferSize];
                do
                {
                    int count;
                    do
                    {
                        if ((count = await client.SslStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            if (count <= 0)
                            {
                                if (currentTimeout < maxTimeout)
                                {
                                    currentTimeout += sleepInterval;
                                    await Task.Delay(sleepInterval);
                                }
                                else
                                    goto label_37;
                            }
                            else
                                goto label_33;
                        }
                        else
                            break;
                    } while (!timeout);

                    break;
                    label_33:
                    dataMs.Write(buffer, 0, count);
                    bytesRead += count;
                    bytesRemaining -= (long) count;
                    currentTimeout = 0;
                    if (bytesRemaining < bufferSize)
                        bufferSize = bytesRemaining;
                    buffer = new byte[bufferSize];
                } while (bytesRemaining != 0L && (long) bytesRead != contentLength);

                goto label_42;
                label_37:
                timeout = true;
                label_42:
                if (timeout)
                {
                    Server.Log("*** MessageReadAsync timeout " + (object) currentTimeout + "ms/" +
                                           (object) maxTimeout + "ms exceeded while reading content after reading " +
                                           (object) bytesRead + " bytes");
                    return (byte[]) null;
                }

                array1 = dataMs.ToArray();
                buffer = (byte[]) null;
            }

            if (array1 == null || array1.Length < 1)
            {
                Server.Log("*** MessageReadAsync " + client.IpPort + " no content read");
                return (byte[]) null;
            }

            if ((long) array1.Length == contentLength)
                return array1;
            Server.Log("*** MessageReadAsync " + client.IpPort + " content length " +
                                   (object) array1.Length + " bytes does not match header value " +
                                   (object) contentLength + ", discarding");
            return (byte[]) null;
        }
    }
    
    public class ClientMetadata : IDisposable
    {
        private bool disposed;
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private SslStream sslStream;
        private string ipPort;

        public ClientMetadata(TcpClient tcp)
        {
            TcpClient tcpClient = tcp;
            if (tcpClient == null)
                throw new ArgumentNullException(nameof (tcp));
            this.tcpClient = tcpClient;
            this.networkStream = tcp.GetStream();
            this.ipPort = tcp.Client.RemoteEndPoint.ToString();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object) this);
        }

        public TcpClient TcpClient
        {
            get
            {
                return this.tcpClient;
            }
        }

        public NetworkStream NetworkStream
        {
            get
            {
                return this.networkStream;
            }
        }

        public SslStream SslStream
        {
            get
            {
                return this.sslStream;
            }
            set
            {
                this.sslStream = value;
            }
        }

        public string IpPort
        {
            get
            {
                return this.ipPort;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;
            if (disposing)
            {
                if (this.sslStream != null)
                    this.sslStream.Close();
                if (this.networkStream != null)
                    this.networkStream.Close();
                if (this.tcpClient != null)
                    this.tcpClient.Close();
            }
            this.disposed = true;
        }
    }
}
