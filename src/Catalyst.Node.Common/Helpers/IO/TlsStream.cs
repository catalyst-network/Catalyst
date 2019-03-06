using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Dawn;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO
{
    public static class TlsStream
    {
        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        private static bool AcceptCertificate(object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static SslStream GetTlsStream(NetworkStream networkStream,
            int direction,
            X509Certificate sslCertificate,
            bool acceptInvalidCerts,
            bool mutuallyAuthenticate = false)
        {
            return GetTlsStream(networkStream, direction, sslCertificate, acceptInvalidCerts, mutuallyAuthenticate,
                null);
        }

        public static SslStream GetTlsStream(NetworkStream networkStream,
            int direction,
            X509Certificate sslCertificate,
            bool acceptInvalidCerts,
            IPEndPoint endPoint)
        {
            return GetTlsStream(networkStream, direction, sslCertificate, acceptInvalidCerts, false, endPoint);
        }

        /// <summary>
        ///     inbound connections = 1, outbound connections = 2
        /// </summary>
        /// <param name="networkStream"></param>
        /// <param name="direction"></param>
        /// <param name="sslCertificate"></param>
        /// <param name="acceptInvalidCerts"></param>
        /// <param name="mutuallyAuthenticate"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        /// <exception cref="AuthenticationException"></exception>
        public static SslStream GetTlsStream(NetworkStream networkStream,
            int direction,
            X509Certificate sslCertificate,
            bool acceptInvalidCerts,
            bool mutuallyAuthenticate,
            IPEndPoint endPoint)
        {
            Guard.Argument(networkStream, nameof(networkStream)).NotNull();
            Guard.Argument(sslCertificate, nameof(sslCertificate)).NotNull();

            var certificateCollection = new X509CertificateCollection {sslCertificate};

            var sslStream = acceptInvalidCerts
                ? new SslStream(networkStream, false, AcceptCertificate)
                : new SslStream(networkStream, false);

            try
            {
                switch (direction)
                {
                    case 1:
                        sslStream.AuthenticateAsServer(sslCertificate, true, SslProtocols.Tls12, false);
                        break;
                    case 2 when endPoint != null:
                        sslStream.AuthenticateAsClient(
                            endPoint.Address.ToString() ??
                            throw new ArgumentNullException(nameof(endPoint.Address)),
                            certificateCollection,
                            SslProtocols.Tls12,
                            acceptInvalidCerts
                        );
                        break;
                    case 2:
                        throw new ArgumentNullException(nameof(endPoint), "need endpoint for outbound connections");
                    default:
                        throw new Exception(
                            "logically you should never get here, so here is a un-useful error message");
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

                return sslStream;
            }
            catch (Exception e)
            {
                Log.Error("CreateTlsStream: Exception", e);
                sslStream.Dispose();
                return null;
            }
        }
    }
}