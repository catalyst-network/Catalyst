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
using Catalyst.Helpers.Workers;
using Dawn;
using DnsClient.Protocol;

namespace Catalyst.Node.Modules.Core.P2P
{
    public class P2P : IDht, IDisposable
    {
        private static readonly object Mutex = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="publicKey"></param>
        /// <param name="acceptInvalidCerts"></param>
        /// <param name="mutualAuthentication"></param>
        /// <param name="debug"></param>
        private P2P(
 
        )
        {
//            Guard.Argument(dataDir, nameof(dataDir)).NotNull();
            
//            try
//            {
//                NodeIdentity = PeerIdentifier.BuildPeerId(publicKey,
//                    EndpointBuilder.BuildNewEndPoint(p2PSettings.BindAddress, p2PSettings.Port));
//            }
//            catch (ArgumentNullException e)
//            {
//                LogException.Message("Catalyst.Helpers.Network GetInstance", e);
//                return;
//            }

//            if (BannedIps?.Count > 0) BannedIps = new List<string>(bannedIps);

//            AcceptInvalidCerts = acceptInvalidCerts; //@TODO put this in settings
//            MutuallyAuthenticate = mutualAuthentication; //@TODO put this in settings
            CancellationToken = new CancellationTokenSource();
            Token = CancellationToken.Token;
            
            try
            {
                SeedNodes = new List<IPEndPoint>();
//                GetSeedNodes(p2PSettings);
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
            
//            Task.Run(async () =>
//                await PeerManager.InboundConnectionListener(
//                    new IPEndPoint(IPAddress.Parse(p2PSettings.BindAddress),
//                        p2PSettings.Port
//                    )
//                )
//            );
        }

        private bool Debug { get; }
        private CancellationToken Token { get; }
        private List<IPAddress> BannedIps { get; } //@TODO revist this
        private bool AcceptInvalidCerts { get; }
        internal PeerManager PeerManager { get; set; }
        private static P2P Instance { get; set; }
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
        internal void GetSeedNodes(List<string> seedServers)
        {
            var dnsQueryAnswers = Helpers.Network.Dns.GetTxtRecords(seedServers);
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
            Guard.Argument(sender, nameof(sender)).NotNull();
            Guard.Argument(e, nameof(e)).NotNull();
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
//        public static P2P GetInstance(string dataDir, byte[] publicKey)
        public static P2P GetInstance()
        {
//            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
//            Guard.Argument(publicKey, nameof(publicKey)).NotNull().NotEmpty();

            if (Instance != null) return Instance;
            lock (Mutex)
            {
                if (Instance == null)
                    Instance = new P2P();
            }
            return Instance;
        }
    }
}