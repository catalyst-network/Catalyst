using System;
using System.Net.Sockets;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Modules.Consensus;
using Catalyst.Node.Common.Modules.Contract;
using Catalyst.Node.Common.Modules.Dfs;
using Catalyst.Node.Common.Modules.Gossip;
using Catalyst.Node.Common.Modules.Ledger;
using Catalyst.Node.Common.Modules.Mempool;
using Catalyst.Node.Core.Events;
using Dawn;
using Serilog;

namespace Catalyst.Node.Core
{
    public class CatalystNode : IDisposable, ICatalystNode
    {
        private readonly IConsensus _consensus;
        private readonly IContract _contract;
        private readonly IDfs _dfs;
        private readonly IGossip _gossip;
        private readonly ILedger _ledger;
        private readonly ILogger _logger;
        private readonly IMempool _mempool;
        private readonly IP2P _p2p;

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
        }

        public void Dispose() { Dispose(true); }

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
            var announcePackage = ByteUtil.Merge(network, _p2p.Identifier.Id);
            _logger.Debug(string.Join(" ", announcePackage));
            nwStream.Write(announcePackage, 0, announcePackage.Length);
            client.Close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _logger.Verbose("Disposing of CatalystNode");
                _disposed = true;
                _logger.Verbose("CatalystNode disposed");
            }
        }
    }
}