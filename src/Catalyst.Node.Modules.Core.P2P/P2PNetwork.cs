using System;
using System.Net;
using Catalyst.Helpers.Network;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Sockets;
using Catalyst.Node.Modules.Core.P2P.Peer;
using Catalyst.Node.Modules.Core.P2P.Workers;
using Catalyst.Node.Modules.Core.P2P.Messages;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Helpers.Logger;
using Catalyst.Node.Modules.Core.P2P.Events;
using Catalyst.Helpers.RLP;
using Catalyst.Helpers.Util;

namespace Catalyst.Node.Modules.Core.P2P
{
    public class P2PNetwork : IDht, IDisposable
    {
        private bool Debug { get; set; }
        private CancellationToken Token { get; }
        private List<string> BannedIps { get; set; } //@TODO revist this
        private bool AcceptInvalidCerts { get; set; }
        internal PeerManager PeerManager { get; set; }
        private static P2PNetwork Instance { get; set; }
        private bool MutuallyAuthenticate { get; set; }
        private X509Certificate2 SslCertificate { get; }
        private PeerIdentifier NodeIdentity { get; set; }
        private static readonly object Mutex = new object();        
        private CancellationTokenSource CancellationToken { get; set; }
        private X509Certificate2Collection SslCertificateCollection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip2PSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static P2PNetwork GetInstance(IP2PSettings p2PSettings, string dataDir, byte[] publicKey)
        {
            //@TODO guard util
            if (p2PSettings == null) throw new ArgumentNullException(nameof(p2PSettings));
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));
            if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));
            if (string.IsNullOrEmpty(dataDir))
                throw new ArgumentException("Value cannot be null or empty.", nameof(dataDir));
            if (string.IsNullOrWhiteSpace(dataDir))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(dataDir));

            if (Instance != null) return Instance;
            lock (Mutex)
            {
                if (Instance == null)
                {
                    // ms x509 facility generates invalid x590 certs (ofc ms!!!) have to accept invalid certs for now.
                    // @TODO revist this once we re-write the current ssl layer to use bouncy castle.
                    // @TODO revist permitted ips
                    //@TODO get debug value from what pass in at initialisation of application.
                    Instance = new P2PNetwork(
                       p2PSettings,
                        dataDir,
                        publicKey,
                        true,
                        false,
                        null,
                        true
                    );
                }
            }
            return Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip2PSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <param name="publicKey"></param>
        /// <param name="acceptInvalidCerts"></param>
        /// <param name="mutualAuthentication"></param>
        /// <param name="bannedIps"></param>
        /// <param name="debug"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private P2PNetwork (
            IP2PSettings p2PSettings,
            string dataDir,
            byte[] publicKey,
            bool acceptInvalidCerts,
            bool mutualAuthentication,
            IEnumerable<string> bannedIps, // @TODO do we want this as a parameter can we not pull it once obj is instantiated
            bool debug
        )
        {
            //@TODO guard util
            if (dataDir == null) throw new ArgumentNullException(nameof (dataDir));
            if (p2PSettings == null) throw new ArgumentNullException(nameof (p2PSettings));
            
            // don't let me run on privileged ports, I shouldn't be started as root!!!!
            if (!Ip.ValidPortRange(p2PSettings.Port))
            {
                throw new ArgumentOutOfRangeException(nameof (p2PSettings.Port));
            }
            
            SslCertificate = String.IsNullOrEmpty(p2PSettings.SslCertPassword) ? new X509Certificate2(dataDir+"/"+p2PSettings.PfxFileName) : new X509Certificate2(dataDir+"/"+p2PSettings.PfxFileName, p2PSettings.SslCertPassword);           
            
            try
            {
                NodeIdentity = PeerIdentifier.BuildPeerId(publicKey, new IPEndPoint(IPAddress.Parse(p2PSettings.BindAddress), p2PSettings.Port));
            }
            catch (ArgumentNullException e)
            {
                LogException.Message("Catalyst.Helpers.Network GetInstance", e);
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
            CancellationToken = new CancellationTokenSource();
            Token = CancellationToken.Token;
            PeerManager = new PeerManager(SslCertificate, new PeerList(new ClientWorker()), new MessageQueueManager(), NodeIdentity);
            PeerManager.AnnounceNode += Announce;

            Task.Run(async () => 
                await PeerManager.InboundConnectionListener(
                    new IPEndPoint(IPAddress.Parse(p2PSettings.BindAddress),
                        p2PSettings.Port
                    )
                )
            );
        }

        /// <summary>
        /// @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        bool IDht.Ping()
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        bool IDht.Store(string k, byte[] v)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        List<IPeerIdentifier> IDht.FindNode()
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Announce(object sender, AnnounceNodeEventArgs e)
        {
            //@TODO guard util
            TcpClient client = new TcpClient("192.168.1.213", 21420); //@TODO get seed tracker from config
            NetworkStream nwStream = client.GetStream();
            byte[] network = new byte[1];
            network[0] = 0x01;
            Log.ByteArr(network);
            byte[] announcePackage = ByteUtil.Merge(network, NodeIdentity.Id);
            Log.ByteArr(announcePackage);
            nwStream.Write(announcePackage, 0, announcePackage.Length);
            client.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            CancellationToken.Cancel();
            PeerManager.Dispose();
        }
    }
}
