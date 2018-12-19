using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Concurrent;
using ADL.Node.Core.Modules.Network.Workers;
using System.Security.Cryptography.X509Certificates;
using ADL.Node.Core.Modules.Network.Listeners;
using ADL.Node.Core.Modules.Network.Peer;

namespace ADL.Node.Core.Modules.Network
{
    public class Network : IDHT
    {
        private bool Debug { get; set; }
        private CancellationToken Token { get; }
        private List<string> BannedIps { get; set; } //@TODO revist this
        private bool AcceptInvalidCerts { get; set; }
        private static Network Instance { get; set; }
        private bool MutuallyAuthenticate { get; set; }
        private X509Certificate2 SslCertificate { get; }
        private PeerIdentifier NodeIdentity { get; set; }
        private static readonly object Mutex = new object();        
        private CancellationTokenSource CancellationToken { get; set; }
        private X509Certificate2Collection SslCertificateCollection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="networkSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        public static Network GetInstance(INetworkSettings networkSettings, ISslSettings sslSettings, string dataDir)
        {
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));
            if (sslSettings == null) throw new ArgumentNullException(nameof(sslSettings));
            if (networkSettings == null) throw new ArgumentNullException(nameof(networkSettings));
            
            if (Instance == null)
            {
                lock (Mutex)
                {
                    if (Instance == null)
                    {
                        // ms x509 facility generates invalid x590 certs (ofc ms!!!) have to accept invalid certs for now.
                        // @TODO revist this once we re-write the current ssl layer to use bouncy castle.
                        // @TODO revist permitted ips
                        //@TODO get debug value from what pass in at initialisation of application.
                        Instance = new Network(
                            networkSettings,
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
            return Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="networkSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <param name="acceptInvalidCerts"></param>
        /// <param name="mutualAuthentication"></param>
        /// <param name="bannedIps"></param>
        /// <param name="debug"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private Network (
            INetworkSettings networkSettings,
            ISslSettings sslSettings,
            string dataDir,
            bool acceptInvalidCerts,
            bool mutualAuthentication,
            IEnumerable<string> bannedIps, // do we want this as a parameter can we not pull it once obj is instantiated
            bool debug)
        {
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));
            if (sslSettings == null) throw new ArgumentNullException(nameof(sslSettings));
            if (networkSettings == null) throw new ArgumentNullException(nameof(networkSettings));
            
            // don't let me run on privileged ports, I shouldn't be started as root!!!!
            if (networkSettings.Port < 1024)
            {
                throw new ArgumentOutOfRangeException(nameof(networkSettings.Port));
            }
            
            SslCertificate = String.IsNullOrEmpty(sslSettings.SslCertPassword) ? new X509Certificate2(dataDir+"/"+sslSettings.PfxFileName) : new X509Certificate2(dataDir+"/"+sslSettings.PfxFileName, sslSettings.SslCertPassword);           
            
            try
            {
                byte[] publickey = new byte[1];
                NodeIdentity = new PeerIdentifier(publickey, new IPEndPoint(IPAddress.Parse(networkSettings.BindAddress), networkSettings.Port));
            }
            catch (ArgumentNullException e)
            {
                Log.LogException.Message("Network GetInstance", e);
                return;
            }
            
            if (BannedIps?.Count > 0)
            {
                BannedIps = new List<string>(bannedIps);
            }
            
            SslCertificateCollection = new X509Certificate2Collection
            {
                SslCertificate
            };
            
            Debug = debug;
            AcceptInvalidCerts = acceptInvalidCerts;
            MutuallyAuthenticate = mutualAuthentication;
            
//            Worker = new ClientWorker();
//            SendMessageQueue = new Queue<byte[]>();
//            ReceivedMessageQueue = new Queue<byte[]>();
            CancellationToken = new CancellationTokenSource();
            Token = CancellationToken.Token;
            
            UnIdentifiedConnections = new ConcurrentDictionary<string, Connection>();
        }

        public void ActivateNode()
        {
            Listener = TcpListenerFactory.CreateListener(NodeIdentity.EndPoint);
        }

        /// <summary>
        /// @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        bool IDHT.Ping()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        bool IDHT.Store(string k, byte[] v)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        List<PeerIdentifier> IDHT.FindNode()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        void IDHT.Announce(PeerIdentifier peerIdentifier)
        {
            throw new NotImplementedException();
        }
    }
}
