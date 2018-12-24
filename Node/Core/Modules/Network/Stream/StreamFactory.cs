using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace ADL.Node.Core.Modules.Network.Stream
{
    public static class StreamFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        private static bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// inbound connections = 1, outbound connections = 2
        /// </summary>
        /// <param name="networkStream"></param>
        /// <param name="direction"></param>
        /// <param name="sslCertificate"></param>
        /// <param name="acceptInvalidCerts"></param>
        /// <param name="mutuallyAuthenticate"></param>
        /// <param name="endPoint"></param>
        /// <returns> SslStream </returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        /// <exception cref="AuthenticationException"></exception>
        public static SslStream CreateTlsStream(
            NetworkStream networkStream,
            int direction,
            X509Certificate sslCertificate,
            bool acceptInvalidCerts,
            bool mutuallyAuthenticate = false,
            IPEndPoint endPoint = null
        )
        {
            if (networkStream == null) throw new ArgumentNullException(nameof(networkStream));
            if (sslCertificate == null) throw new ArgumentNullException(nameof(sslCertificate));
            X509CertificateCollection certificateCollection = new X509CertificateCollection {sslCertificate};

            var sslStream = acceptInvalidCerts ? new SslStream(networkStream, false, new RemoteCertificateValidationCallback(AcceptCertificate)) : new SslStream(networkStream, false);

            try
            {
                switch (direction)
                {
                    case 1:
                        sslStream.AuthenticateAsServer(sslCertificate, true, SslProtocols.Tls12, false);
                        break;
                    case 2 when endPoint != null:
                        sslStream.AuthenticateAsClient(
                            endPoint.Address.ToString() ?? throw new ArgumentNullException(nameof(endPoint.Address)),
                            certificateCollection,
                            SslProtocols.Tls12,
                            !acceptInvalidCerts
                        );
                        break;
                    case 2:
                        throw new Exception("need endpoint for outbound connections");
                    default:
                        throw new Exception("logically you should never get here, so here is a un-useful error message");
                }
                if (!sslStream.IsEncrypted)
                {
                    sslStream.Dispose();
                    throw new AuthenticationException("*** ssl stream not encrypted");
                }
                if (!sslStream.IsAuthenticated)
                {
                    sslStream.Dispose();
                    throw new AuthenticationException("*** ssl stream not authenticated");
                }
                if (mutuallyAuthenticate && !sslStream.IsMutuallyAuthenticated)
                {
                    sslStream.Dispose();
                    throw new AuthenticationException("*** ssl stream failed mutual authentication");
                }
            }
            catch (IOException ex)
            {
                // Some type of problem initiating the SSL connection
                switch (ex.Message)
                {
                    case "Authentication failed because the remote party has closed the transport stream.":
                    case "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.":
                        Log.Log.Message("*** ssl stream IOException closed the connection.");
                        break;
                    case "The handshake failed due to an unexpected packet format.":
                        Log.Log.Message("*** ssl stream IOException disconnected, invalid handshake.");
                        break;
                    default:
                        Log.Log.Message("*** ssl stream IOException from " + Environment.NewLine + ex);
                        break;
                }
                sslStream.Dispose();
                return null;
            }
            catch (Exception ex)
            {
                Log.Log.Message("*** StartInboundTls Exception from " + Environment.NewLine + ex);
                sslStream.Dispose();
                return null;
            }
            Log.Log.Message("Returning valid ssl stream");
            return sslStream;
        }
    }
}