#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions;
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Contract;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Ledger;
using Catalyst.Core.Mempool.Documents;
using Catalyst.Core.P2P;
using Serilog;

namespace Catalyst.Node.POA.CE
{
    public class CatalystNodePoa : ICatalystNode
    {
        public IConsensus Consensus { get; }
        private readonly IContract _contract;
        private readonly IDfs _dfs;
        private readonly ILedger _ledger;
        private readonly IKeySigner _keySigner;
        private readonly ILogger _logger;
        private readonly IMempool<MempoolDocument> _mempool;
        private readonly IPeerService _peer;
        private readonly IRpcServer _rpcServer;
        private readonly IPeerClient _peerClient;
        private readonly IPeerSettings _peerSettings;

        public CatalystNodePoa(IKeySigner keySigner,
            IPeerService peer,
            IConsensus consensus,
            IDfs dfs,
            ILedger ledger,
            ILogger logger,
            IRpcServer rpcServer,
            IPeerClient peerClient,
            IPeerSettings peerSettings,
            IMempool<MempoolDocument> mempool,
            IContract contract = null)
        {
            _peer = peer;
            _peerClient = peerClient;
            _peerSettings = peerSettings;
            Consensus = consensus;
            _dfs = dfs;
            _ledger = ledger;
            _keySigner = keySigner;
            _logger = logger;
            _rpcServer = rpcServer;
            _mempool = mempool;
            _contract = contract;
        }

        public async Task StartSockets()
        {
            await _rpcServer.StartAsync().ConfigureAwait(false);
            await _peerClient.StartAsync().ConfigureAwait(false);
            await _peer.StartAsync().ConfigureAwait(false);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            _logger.Information("Starting the Catalyst Node");
            _logger.Information("using PeerIdentifier: {0}", new PeerIdentifier(_peerSettings));

            await StartSockets().ConfigureAwait(false);
            Consensus.StartProducing();

            bool exit;

            do
            {
                await Task.Delay(300, ct); //just to get the exit message at the bottom

                _logger.Debug("Type 'exit' to exit, anything else to continue");
                exit = string.Equals(Console.ReadLine(), "exit", StringComparison.OrdinalIgnoreCase);
            } while (!ct.IsCancellationRequested && !exit);

            _logger.Debug("Stopping the Catalyst Node");
        }
    }
}
