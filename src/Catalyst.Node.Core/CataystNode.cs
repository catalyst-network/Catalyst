using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Helpers.Cryptography;
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
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messages;
using Dawn;
using Serilog;
using Dns = Catalyst.Node.Core.Helpers.Network.Dns;

namespace Catalyst.Node.Core
{
    public class CatalystNode : IDisposable, ICatalystNode
    {
        private readonly IP2P _p2p;
        private readonly IConsensus _consensus;
        private readonly IDfs _dfs;
        private readonly ILedger _ledger;
        private readonly ILogger _logger;
        private readonly IMempool _mempool;
        private readonly IContract _contract;
        private readonly IGossip _gossip;
        private readonly PeerIdentifier _peerIdentifier;

        private bool _disposed;

        public CatalystNode(IP2P p2p,
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
            ConnectionManager = new ConnectionManager(certificateStore.ReadOrCreateCertificateFile(p2p.Settings.PfxFileName),
                new PeerList(new ClientWorker()),
                new MessageQueueManager(),
                //Todo: use NSec here to convert key to bytes
                _peerIdentifier
            );

            Task.Run(async () =>
            {
                await ConnectionManager.InboundConnectionListener(p2p.Settings.EndPoint);
            });

            ConnectionManager.AnnounceNode += Announce;
        }

        public ConnectionManager ConnectionManager { get; }


        /// <summary>
        /// </summary>
        /// <returns></returns>
        public void Announce(object sender, AnnounceNodeEventArgs e)
        {
            Guard.Argument(sender, nameof(sender)).NotNull();
            Guard.Argument(e, nameof(e)).NotNull();
            var client = new TcpClient(_p2p.Settings.AnnounceServer.Address.ToString(),
                _p2p.Settings.AnnounceServer.Port);
            var nwStream = client.GetStream();
            var network = new byte[1];
            network[0] = 0x01;
            _logger.Debug(string.Join(" ", network));
            var announcePackage = ByteUtil.Merge(network, _peerIdentifier.Id);
            _logger.Debug(string.Join(" ", announcePackage));
            nwStream.Write(announcePackage, 0, announcePackage.Length);
            client.Close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _logger.Verbose("Disposing of CatalystNode");
                ConnectionManager?.Dispose();
                _logger.Verbose("CatalystNode disposed");
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
