using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Helpers.Logger;
using Catalyst.Helpers.Network;
using Catalyst.Helpers.Util;
using Catalyst.Node.Modules.Core.P2P.Events;
using Catalyst.Node.Modules.Core.P2P.Messages;
using Catalyst.Node.Modules.Core.P2P.Peer;
using Catalyst.Node.Modules.Core.P2P.Workers;
using DnsClient.Protocol;

namespace Catalyst.Node.Modules.Core.P2P
{
    public class P2PNetwork : IDht, IDisposable
    {
        private static readonly object Mutex = new object();

        /// <summary>
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
        private P2PNetwork(
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
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));
            if (p2PSettings == null) throw new ArgumentNullException(nameof(p2PSettings));

            // don't let me run on privileged ports, I shouldn't be started as root!!!!
            if (!Ip.ValidPortRange(p2PSettings.Port)) throw new ArgumentOutOfRangeException(nameof(p2PSettings.Port));

            SslCertificate = string.IsNullOrEmpty(p2PSettings.SslCertPassword)
                ? new X509Certificate2(dataDir + "/" + p2PSettings.PfxFileName)
                : new X509Certificate2(dataDir + "/" + p2PSettings.PfxFileName, p2PSettings.SslCertPassword);

            SslCertificateCollection = new X509Certificate2Collection
            {
                SslCertificate
            };
            
            try
            {
                NodeIdentity = PeerIdentifier.BuildPeerId(publicKey,
                    EndpointBuilder.BuildNewEndPoint(p2PSettings.BindAddress, p2PSettings.Port));
            }
            catch (ArgumentNullException e)
            {
                LogException.Message("Catalyst.Helpers.Network GetInstance", e);
                return;
            }

//            if (BannedIps?.Count > 0) BannedIps = new List<string>(bannedIps);

            Debug = debug;// @todo get from node options
            AcceptInvalidCerts = acceptInvalidCerts; //@TODO put this in settings
            MutuallyAuthenticate = mutualAuthentication; //@TODO put this in settings
            CancellationToken = new CancellationTokenSource();
            Token = CancellationToken.Token;
            
            try
            {
                SeedNodes = new List<IPEndPoint>();
                GetSeedNodes(p2PSettings);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            PeerManager = new PeerManager(SslCertificate, new PeerList(new ClientWorker()), new MessageQueueManager(),
                NodeIdentity); //@TODO DI inject this from autofac
            
//            PeerManager.PeerList.
            
            PeerManager.AnnounceNode += Announce;
            
            Task.Run(async () =>
                await PeerManager.InboundConnectionListener(
                    new IPEndPoint(IPAddress.Parse(p2PSettings.BindAddress),
                        p2PSettings.Port
                    )
                )
            );
        }

        private bool Debug { get; }
        private CancellationToken Token { get; }
        private List<IPAddress> BannedIps { get; } //@TODO revist this
        private bool AcceptInvalidCerts { get; }
        internal PeerManager PeerManager { get; set; }
        private static P2PNetwork Instance { get; set; }
        private bool MutuallyAuthenticate { get; }
        private List<IPEndPoint> SeedNodes { get; }
        private X509Certificate2 SslCertificate { get; }
        private PeerIdentifier NodeIdentity { get; }
        private CancellationTokenSource CancellationToken { get; }
        private X509Certificate2Collection SslCertificateCollection { get; }

        /// <summary>
        ///     @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        bool IDht.Ping(PeerIdentifier queryingNode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        bool IDht.Store(string k, byte[] v)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        dynamic IDht.FindValue(string k)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  If a corresponding value is present on the queried node, the associated data is returned.
        ///  Otherwise the return value is the return equivalent to FindNode()
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        List<PeerIdentifier> IDht.FindNode(PeerIdentifier queryingNode, PeerIdentifier targetNode)
        {
            // @TODO just to satisfy the DHT interface, need to implement
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        List<PeerIdentifier> IDht.GetPeers(PeerIdentifier queryingNode)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="queryingNode"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        List<PeerIdentifier> IDht.PeerExchange(PeerIdentifier queryingNode)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// </summary>
        public void Dispose()
        {
            //@TODO add gc supress dispose
            CancellationToken.Cancel();
            PeerManager.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p2PSettings"></param>
        internal void GetSeedNodes(IP2PSettings p2PSettings)
        {
            var dnsQueryAnswers = Helpers.Network.Dns.GetTxtRecords(p2PSettings.SeedList);
            foreach (var dnsQueryAnswer in dnsQueryAnswers)
            {
                var answerSection = (TxtRecord) dnsQueryAnswer.Answers.FirstOrDefault();
                SeedNodes.Add(EndpointBuilder.BuildNewEndPoint(answerSection.EscapedText.FirstOrDefault()));
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Announce(object sender, AnnounceNodeEventArgs e)
        {
            //@TODO guard util
            var client = new TcpClient("192.168.1.213", 21420); //@TODO get seed tracker from config
            var nwStream = client.GetStream();
            var network = new byte[1];
            network[0] = 0x01;
            Log.ByteArr(network);
            var announcePackage = ByteUtil.Merge(network, NodeIdentity.Id);
            Log.ByteArr(announcePackage);
            nwStream.Write(announcePackage, 0, announcePackage.Length);
            client.Close();
        }

        /// <summary>
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
            return Instance;
        }
    }
}