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
using Autofac;
using Catalyst.Common.Interfaces;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.Modules.Contract;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.Modules.Ledger;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.P2P;
using Catalyst.Modules.Lib.Api;
using Serilog;

namespace Catalyst.Node.POA.CE
{
    public class CatalystNodePoa
        : ICatalystNode
    {
        public IConsensus Consensus { get; }
        private readonly IContract _contract;
        private readonly IDfs _dfs;
        private readonly ILedger _ledger;
        private readonly IKeySigner _keySigner;
        private readonly ILogger _logger;
        private readonly IMempool _mempool;
        private readonly IPeerService _peer;
        private readonly INodeRpcServer _nodeRpcServer;
        private readonly IPeerClient _peerClient;
        private readonly IPeerSettings _peerSettings;

        private readonly IApi _api;

        public CatalystNodePoa(IKeySigner keySigner,
            IPeerService peer,
            IConsensus consensus,
            IDfs dfs,
            ILedger ledger,
            ILogger logger,
            INodeRpcServer nodeRpcServer,
            IApi api,
            IPeerClient peerClient,
            IPeerSettings peerSettings,
            IMempool mempool = null,
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
            _nodeRpcServer = nodeRpcServer;
            _api = api;
            _mempool = mempool;
            _contract = contract;
        }

        public async Task StartSockets(IContainer serviceProvider)
        {
            await _nodeRpcServer.StartAsync().ConfigureAwait(false);
            await _peerClient.StartAsync().ConfigureAwait(false);
            await _peer.StartAsync().ConfigureAwait(false);
            await _api.StartApiAsync(serviceProvider).ConfigureAwait(false);
        }

        public async Task RunAsync(CancellationToken ct, IContainer serviceProvider)
        {
            _logger.Information("Starting the Catalyst Node");
            _logger.Information("using PeerIdentifier: {0}", new PeerIdentifier(_peerSettings));

            await StartSockets(serviceProvider).ConfigureAwait(false);
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
