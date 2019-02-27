using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Cryptography;
using Catalyst.Node.Common.Modules.Consensus;
using Catalyst.Node.Common.Modules.Contract;
using Catalyst.Node.Common.Modules.Dfs;
using Catalyst.Node.Common.Modules.Gossip;
using Catalyst.Node.Common.Modules.Ledger;
using Catalyst.Node.Common.Modules.Mempool;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Core.Events;
using Catalyst.Node.Core.Helpers.Util;
using Catalyst.Node.Core.Helpers.Workers;
using Catalyst.Node.Core.Modules.P2P.Messages;
using Catalyst.Node.Core.P2P;
using Dawn;
using Serilog;
using Dns = Catalyst.Node.Core.Helpers.Network.Dns;

namespace Catalyst.Node.Core
{
    public class CatalystNode : IDisposable
    {
        private readonly IP2P _p2p;
        private readonly IConsensus _consensus;
        private readonly IDfs _dfs;
        private readonly ILedger _ledger;
        private readonly ILogger _logger;
        private readonly IMempool _mempool;
        private readonly IContract _contract;
        private readonly IGossip _gossip;
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private bool _disposed;
        private PeerIdentifier _peerIdentifier;

        /// <summary>
        ///     Instantiates basic CatalystSystem.
        /// </summary>
        private CatalystNode(IP2P p2p,
            ICertificateStore certificateStore,
            IConsensus consensus, 
            IDfs dfs,
            ILedger ledger,
            ILogger logger,
            IMempool mempool = null,
            IContract contract = null, 
            IGossip gossip = null)
        {
            _p2p = p2p;
            _consensus = consensus;
            _dfs = dfs;
            _ledger = ledger;
            _logger = logger;
            _mempool = mempool;
            _contract = contract;
            _gossip = gossip;

            var dns = new Dns(p2p.Settings.DnsServer);
            var ipEndPoint = new IPEndPoint(p2p.Settings.BindAddress, p2p.Settings.Port);
            _peerIdentifier = new PeerIdentifier(Encoding.UTF8.GetBytes(p2p.Settings.PublicKey), ipEndPoint);
            ConnectionManager = new ConnectionManager(certificateStore.GetCertificateFromFile(p2p.Settings.PfxFileName),
                new PeerList(new ClientWorker()),
                new MessageQueueManager(),
                //Todo: use NSec here to convert key to bytes
                _peerIdentifier
            );

            Task.Run(async () =>
            {
                await ConnectionManager.InboundConnectionListener(ipEndPoint);
            });

            ConnectionManager.AnnounceNode += Announce;
        }

        private static CatalystNode Instance { get; set; }
        private ConnectionManager ConnectionManager { get; }

        
        /// <summary>
        ///     If a corresponding value is present on the queried node, the associated data is returned.
        ///     Otherwise the return value is the return equivalent to FindNode()
        /// </summary>
        /// <param name="k"></param>
        /// <param name="queryingNode"></param>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        public List<IPeerIdentifier> FindNode(IPeerIdentifier queryingNode, IPeerIdentifier targetNode)
        {
            // @TODO just to satisfy the DHT interface, need to implement
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public List<IPeerIdentifier> GetPeers(IPeerIdentifier queryingNode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="queryingNode"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public List<IPeerIdentifier> PeerExchange(IPeerIdentifier queryingNode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private void Announce(object sender, AnnounceNodeEventArgs e)
        {
            Guard.Argument(sender, nameof(sender)).NotNull();
            Guard.Argument(e, nameof(e)).NotNull();
            var client = new TcpClient(_p2p.Settings.AnnounceServer.Address.ToString(),
                _p2p.Settings.AnnounceServer.Port);
            var nwStream = client.GetStream();
            var network = new byte[1];
            network[0] = 0x01;
            Logger.Debug(string.Join(" ", network));
            var announcePackage = ByteUtil.Merge(network, _peerIdentifier.Id);
            Logger.Debug(string.Join(" ", announcePackage));
            nwStream.Write(announcePackage, 0, announcePackage.Length);
            client.Close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                Logger.Verbose("Disposing of CatalystNode");
                ConnectionManager?.Dispose();
                Logger.Verbose("CatalystNode disposed");
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
