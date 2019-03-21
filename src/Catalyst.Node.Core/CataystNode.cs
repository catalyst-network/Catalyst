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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Modules.Consensus;
using Catalyst.Node.Common.Interfaces.Modules.Contract;
using Catalyst.Node.Common.Interfaces.Modules.Dfs;
using Catalyst.Node.Common.Interfaces.Modules.KeySigner;
using Catalyst.Node.Common.Interfaces.Modules.Ledger;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
using Catalyst.Node.Core.Events;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Transaction;
using Dawn;
using Google.Protobuf;
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
        private readonly IP2P _p2P;
        private readonly IRpcServer _rpcServer;
       
        private bool _disposed;

        public CatalystNode(
            IP2P p2P,
            ICertificateStore certificateStore,
            IConsensus consensus,
            IDfs dfs,
            ILedger ledger,
            IKeySigner keySigner,
            ILogger logger,
            IRpcServer rpcServer,
            IMempool mempool = null,
            IContract contract = null
            )
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

            // await _dfs.StartAsync(ct);
            _logger.Information("Starting the Catalyst Node");
            bool exit = false;
            do
            {
                _logger.Information("Creating a Transaction message");
                _logger.Information("Please type in a pubkey for the transaction signature");
                var pubkey = Console.ReadLine();

                _logger.Information("Please type in a transaction version");
                if (!uint.TryParse(Console.ReadLine(), out var version))
                {
                    version = 1;
                }
                var tx = new Transaction { Version = version, Signature = new TransactionSignature {Signature = ByteString.CopyFromUtf8(pubkey)}};

                await _p2P.Messaging.BroadcastMessageAsync(tx.ToAny());
                await Task.Delay(300, ct); //just to get the next message at the bottom

                _logger.Information("Creating a Ping message");
                _logger.Information("Please type in a ping message content");
                var ping = new PeerProtocol.Types.PingRequest { Ping = Console.ReadLine() };

                await _p2P.Messaging.BroadcastMessageAsync(ping.ToAny());
                await Task.Delay(300, ct); //just to get the exit message at the bottom
                
                _logger.Information("Type 'exit' to exit, anything else to continue");
                exit = string.Equals(Console.ReadLine(), "exit", StringComparison.OrdinalIgnoreCase);

            } while (!ct.IsCancellationRequested && !exit);

            _logger.Information("Stopping the Catalyst Node");
        }
        
        /// <summary>
        /// </summary>
        /// <returns></returns>
        private void Announce(object sender, AnnounceNodeEventArgs e)
        {
            Guard.Argument(sender, nameof(sender)).NotNull();
            Guard.Argument(e, nameof(e)).NotNull();
            var client = new TcpClient(_p2P.Settings.AnnounceServer.Address.ToString(),
                _p2P.Settings.AnnounceServer.Port);
            var nwStream = client.GetStream();
            var network = new byte[1];
            network[0] = 0x01;
            _logger.Debug(string.Join(" ", network));
            var announcePackage = ByteUtil.Merge(network, _p2P.Identifier.Id);
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