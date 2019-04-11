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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Modules.Consensus;
using Catalyst.Node.Common.Interfaces.Modules.Contract;
using Catalyst.Node.Common.Interfaces.Modules.Dfs;
using Catalyst.Node.Common.Interfaces.Modules.KeySigner;
using Catalyst.Node.Common.Interfaces.Modules.Ledger;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.Interfaces.Rpc;
using Serilog;

namespace Catalyst.Node.Core
{
    public class CatalystNode : IDisposable, ICatalystNode
    {
        private readonly IConsensus _consensus;
        private readonly IContract _contract;
        private readonly IDfs _dfs;
        private readonly ILedger _ledger;
        private readonly IKeySigner _keySigner;
        private readonly ILogger _logger;
        private readonly IMempool _mempool;
        private readonly IP2PService _p2P;
        private readonly IRpcServer _rpcServer;

        private bool _disposed;

        public CatalystNode(IP2PService p2P,
            ICertificateStore certificateStore,
            IConsensus consensus,
            IDfs dfs,
            ILedger ledger,
            IKeySigner keySigner,
            ILogger logger,
            IRpcServer rpcServer,
            IMempool mempool = null,
            IContract contract = null)
        {
            _p2P = p2P;
            _consensus = consensus;
            _dfs = dfs;
            _ledger = ledger;
            _keySigner = keySigner;
            _logger = logger;
            _rpcServer = rpcServer;
            _mempool = mempool;
            _contract = contract;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            _logger.Information("Starting the Catalyst Node");
            bool exit;
            do
            {
                await Task.Delay(300, ct); //just to get the exit message at the bottom

                _logger.Information("Type 'exit' to exit, anything else to continue");
                exit = string.Equals(Console.ReadLine(), "exit", StringComparison.OrdinalIgnoreCase);
            } while (!ct.IsCancellationRequested && !exit);

            _logger.Information("Stopping the Catalyst Node");
        }

        public void Dispose()
        {
            Dispose(true);
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
