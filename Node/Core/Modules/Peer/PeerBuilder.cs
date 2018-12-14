using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ADL.Node.Core.Modules.Peer.IO;

namespace ADL.Node.Core.Modules.Peer
{
    internal class PeerBuilder : ServerBase
    {
//        private int port;
//        private string ip;
//        private bool _Debug;
//        private TcpClient TcpClient;
//        private SslStream SslStream;
//        private bool Disposed = false;
//        private readonly SemaphoreSlim _SendLock;
//        private X509Certificate2 _SslCertificate;
//        private X509Certificate2Collection _SslCertificateCollection;
        
        public PeerBuilder (
            string ip,
            int port,
            string pfxCertFile,
            string pfxCertPass,
            bool acceptInvalidCerts,
            bool mutualAuthentication,
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

            AcceptInvalidCerts = acceptInvalidCerts;

//            _SendLock = new SemaphoreSlim(1);
            SemaphoreSlim _SendLock = new SemaphoreSlim(1);

            X509Certificate2 sslCertificate = null;
            if (String.IsNullOrEmpty(pfxCertPass))
            {
                sslCertificate = new X509Certificate2(pfxCertFile);
            }
            else
            {
                sslCertificate = new X509Certificate2(pfxCertFile, pfxCertPass);
            }

            X509Certificate2Collection _SslCertificateCollection = new X509Certificate2Collection
            {
                sslCertificate
            };

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

//                _Sourceip = ((ipEndPoint)_Tcp.Client.LocalEndPoint).Address.ToString();
//                _Sourceport = ((ipEndPoint)_Tcp.Client.LocalEndPoint).port;

                SslStream ssl = null;
                if (AcceptInvalidCerts)
                {
                    // accept invalid certs
                    ssl = new SslStream(tcpClient.GetStream(), false, new RemoteCertificateValidationCallback(AcceptCertificate));
                }
                else
                {
                    // do not accept invalid SSL certificates
                    ssl = new SslStream(tcpClient.GetStream(), false);
                }

                ssl.AuthenticateAsClient(ip, _SslCertificateCollection, SslProtocols.Tls12, !AcceptInvalidCerts);

                if (!ssl.IsEncrypted)
                {
                    throw new AuthenticationException("Stream is not encrypted");
                }

                if (!ssl.IsAuthenticated)
                {
                    throw new AuthenticationException("Stream is not authenticated");
                }

                if (mutualAuthentication && !ssl.IsMutuallyAuthenticated)
                {
                    throw new AuthenticationException("Mutual authentication failed");
                }

                _Connected = true;
            }
            
            finally
            {
                wh.Close();
            }

            if (_PeerConnected != null)
            {
                Task.Run(() => _PeerConnected(ip, port));
            }

            Peer client = new Peer(tcpClient);

            _TokenSource = new CancellationTokenSource();
            _Token = _TokenSource.Token;
            Task.Run(async () => await PeerDataReceiver(client, _Token), _Token);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private async Task PeerDataReceiver(Peer client, CancellationToken? cancelToken=null)
        {
            var port = ((IPEndPoint)client.TcpClient.Client.LocalEndPoint).Port;
            var ip = ((IPEndPoint)client.TcpClient.Client.LocalEndPoint).Address.ToString();
            
            try
            {
                while (true)
                {
                    cancelToken?.ThrowIfCancellationRequested();

                    if (client.TcpClient == null)
                    {
                        Log("*** DataReceiver null TCP interface detected, disconnection or close assumed");
                        break;
                    }

                    if (!client.TcpClient.Connected)
                    {
                        Log("*** DataReceiver server " + ip + ":" + port + " disconnected");
                        break;
                    }

                    byte[] data = await MessageReadAsync(client);
                    if (data == null)
                    {
                        await Task.Delay(30);
                        continue;
                    }

                    Task<bool> unawaited = Task.Run(() => _MessageReceived(ip, port, data));
                }
            }
            catch (OperationCanceledException)
            {
                throw; //normal cancellation
            }
            catch (Exception)
            {
                Log("*** DataReceiver server " + ip + ":" + port + " disconnected");
            }
            finally
            {
                _Connected = false;
                _PeerDisconnected?.Invoke(ip, port);
            }
        }

        public override void Dispose()
        {
            if (base.Disposed)
            {
                return;
            }
//            else
//            {
//                if (TcpClient != null)
//                {
//                    if (TcpClient.Connected)
//                    {
//                        NetworkStream ns = TcpClient.GetStream();
//                        if (ns != null)
//                        {
//                            ns.Close();
//                        }
//                    }
//
//                    TcpClient.Close();
//                }
//
//                SslStream.Dispose();
//
//                _TokenSource.Cancel();
//                _TokenSource.Dispose();
//
//                _SendLock.Dispose();

                _Connected = false;   
//            }
//            Disposed = true;
        }
    }

}