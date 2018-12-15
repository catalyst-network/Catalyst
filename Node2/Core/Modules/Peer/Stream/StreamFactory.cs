using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace ADL.Node.Core.Modules.Peer.Stream
{
    public class StreamFactory
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
            // return true; // Allow untrusted certificates.
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
        /// <param name="certificateCollection"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        public static SslStream GetTlsStream(
            NetworkStream networkStream,
            int direction,
            X509Certificate sslCertificate,
            bool acceptInvalidCerts,
            bool mutuallyAuthenticate = false,
            string ip = null,
            int port = 0
        )
        {
//            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
            if (networkStream == null) throw new ArgumentNullException(nameof(networkStream));
            if (sslCertificate == null) throw new ArgumentNullException(nameof(sslCertificate));
            X509CertificateCollection certificateCollection = new X509CertificateCollection {sslCertificate};

            var sslStream = acceptInvalidCerts ? new SslStream(networkStream, false, new RemoteCertificateValidationCallback(AcceptCertificate)) : new SslStream(networkStream, false);

            try
            {
                if (direction == 1)
                {
                    sslStream.AuthenticateAsServer(sslCertificate, true, SslProtocols.Tls12, false);                    
                } 
                else if (direction == 2)
                {
                    sslStream.AuthenticateAsClient(
                        ip ?? throw new ArgumentNullException(nameof(ip)),
                        certificateCollection,
                        SslProtocols.Tls12,
                        !acceptInvalidCerts
                    );
                }
                if (!sslStream.IsEncrypted)
                {
                    sslStream.Dispose();
                    throw new AuthenticationException("*** ssl stream from " + ip + port + " not encrypted");
                }
                if (!sslStream.IsAuthenticated)
                {
                    sslStream.Dispose();
                    throw new AuthenticationException("*** ssl stream from " + ip + port + " not authenticated");
                }
                if (mutuallyAuthenticate && !sslStream.IsMutuallyAuthenticated)
                {
                    sslStream.Dispose();
                    throw new AuthenticationException("*** ssl stream from " + ip + port + " failed mutual authentication");
                }
            }
            catch (IOException ex)
            {
                // Some type of problem initiating the SSL connection
                switch (ex.Message)
                {
                    case "Authentication failed because the remote party has closed the transport stream.":
                    case "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.":
                        Log.Log.Message("*** StartTls IOException " + ip + port + " closed the connection.");
                        break;
                    case "The handshake failed due to an unexpected packet format.":
                        Log.Log.Message("*** StartTls IOException " + ip + port + " disconnected, invalid handshake.");
                        break;
                    default:
                        Log.Log.Message("*** StartTls IOException from " + ip + port + Environment.NewLine + ex.ToString());
                        break;
                }
                sslStream.Dispose();
                return null;
            }
            catch (Exception ex)
            {
                Log.Log.Message("*** StartInboundTls Exception from " + ip + port +  Environment.NewLine + ex.ToString());
                sslStream.Dispose();
                return null;
            }
            Log.Log.Message("Returning valid ssl stream");
            return sslStream;
        }
    }
}