/*
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

using System;
using System.Net.Sockets;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Modules.Consensus;
using Catalyst.Node.Common.Interfaces.Modules.Contract;
using Catalyst.Node.Common.Interfaces.Modules.Dfs;
using Catalyst.Node.Common.Interfaces.Modules.KeySigner;
using Catalyst.Node.Common.Interfaces.Modules.Ledger;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
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
        private readonly ILedger _ledger;
        private readonly IKeySigner _keySigner;
        private readonly ILogger _logger;
        private readonly IMempool _mempool;
        private readonly IP2P _p2p;
        
        private bool _disposed;

        public CatalystNode(IP2P p2p,
            ICertificateStore certificateStore,
            IConsensus consensus,
            IDfs dfs,
            ILedger ledger,
            IKeySigner keySigner,
            ILogger logger,
            IMempool mempool = null,
            IContract contract = null
            )
        {
            _p2p = p2p;
            _consensus = consensus;
            _dfs = dfs;
            _ledger = ledger;
            _keySigner = keySigner;
            _logger = logger;
            _mempool = mempool;
            _contract = contract;
        }

        public void Start()
        {
            while (true)
            {
                Console.WriteLine("*");
            }
        }
        
        /// <summary>
        /// </summary>
        /// <returns></returns>
        private void Announce(object sender, AnnounceNodeEventArgs e)
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