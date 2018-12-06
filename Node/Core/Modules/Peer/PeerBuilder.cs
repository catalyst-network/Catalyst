using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ADL.Node.Core.Modules.Peer.IO;

namespace ADL.Node.Core.Modules.Peer
{
    internal class PeerBuilder : ServerBase, IDisposable
    {
        private int Port;
        private string Ip;
        private bool _Debug;
        private TcpClient TcpClient;
        private SslStream SslStream;
        private bool Disposed = false;
        private readonly SemaphoreSlim _SendLock;
        private X509Certificate2 _SslCertificate;
        private X509Certificate2Collection _SslCertificateCollection;
        
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

            Ip = ip;
            Port = port;
            _Debug = debug;
            AcceptInvalidCerts = acceptInvalidCerts;

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

            TcpClient = new TcpClient();
            IAsyncResult ar = TcpClient.BeginConnect(Ip, Port, null, null);
            WaitHandle wh = ar.AsyncWaitHandle;

            try
            {
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    TcpClient.Close();
                    throw new TimeoutException("Timeout connecting to " + Ip + ":" + Port);
                }

                TcpClient.EndConnect(ar);

//                _SourceIp = ((IPEndPoint)_Tcp.Client.LocalEndPoint).Address.ToString();
//                _SourcePort = ((IPEndPoint)_Tcp.Client.LocalEndPoint).Port;

                if (AcceptInvalidCerts)
                {
                    // accept invalid certs
                    SslStream = new SslStream(TcpClient.GetStream(), false, new RemoteCertificateValidationCallback(AcceptCertificate));
                }
                else
                {
                    // do not accept invalid SSL certificates
                    SslStream = new SslStream(TcpClient.GetStream(), false);
                }

                SslStream.AuthenticateAsClient(Ip, _SslCertificateCollection, SslProtocols.Tls12, !AcceptInvalidCerts);

                if (!SslStream.IsEncrypted)
                {
                    throw new AuthenticationException("Stream is not encrypted");
                }

                if (!SslStream.IsAuthenticated)
                {
                    throw new AuthenticationException("Stream is not authenticated");
                }

                if (mutualAuthentication && !SslStream.IsMutuallyAuthenticated)
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
                Task.Run(() => _PeerConnected(Ip, Port));
            }

            Peer client = new Peer(TcpClient);

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
                        Log("*** DataReceiver server " + Ip + ":" + Port + " disconnected");
                        break;
                    }

                    byte[] data = await MessageReadAsync(client);
                    if (data == null)
                    {
                        await Task.Delay(30);
                        continue;
                    }

                    Task<bool> unawaited = Task.Run(() => _MessageReceived(Ip, Port, data));
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
                _PeerDisconnected?.Invoke(Ip, Port);
            }
        }

        public override void Dispose()
        {
            if (base.Disposed)
            {
                return;
            }
            else
            {
                if (TcpClient != null)
                {
                    if (TcpClient.Connected)
                    {
                        NetworkStream ns = TcpClient.GetStream();
                        if (ns != null)
                        {
                            ns.Close();
                        }
                    }

                    TcpClient.Close();
                }

                SslStream.Dispose();

                _TokenSource.Cancel();
                _TokenSource.Dispose();

                _SendLock.Dispose();

                _Connected = false;   
            }
            Disposed = true;
        }
    }

}