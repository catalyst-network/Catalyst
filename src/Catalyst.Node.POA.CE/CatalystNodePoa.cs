#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions;
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Contract;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Kvm;
using Dawn;
using MultiFormats;
using Serilog;

namespace Catalyst.Node.POA.CE
{
    public sealed class CatalystNodePoa : ICatalystNode
    {
        public IConsensus Consensus { get; }
        private readonly IContract _contract;
        private readonly IDfsService _dfsService;
        private readonly ILedger _ledger;
        private readonly IKeySigner _keySigner;
        private readonly ILogger _logger;
        private readonly IMempool<PublicEntryDao> _memPool;
        private readonly IPeerService _peer;
        private readonly IPeerClient _peerClient;
        private readonly IPeerSettings _peerSettings;
        private readonly IPublicKey _publicKey;
        private readonly ISynchroniser _synchronizer;
        private readonly IPeerRepository _peerRepository;
        private readonly IKeyApi _keyApi;
        private readonly IPeerDiscovery _peerDiscovery;

        public CatalystNodePoa(IKeySigner keySigner,
            IPeerService peer,
            IConsensus consensus,
            IDfsService dfsService,
            ILedger ledger,
            ILogger logger,
            IPeerClient peerClient,
            IPeerSettings peerSettings,
            IMempool<PublicEntryDao> memPool,
            ISynchroniser synchronizer,
            IPeerRepository peerRepository,
            IKeyApi keyApi,
            IPeerDiscovery peerDiscovery,
            IContract contract = null)
        {
            Guard.Argument(peerRepository, nameof(peerRepository)).NotNull();

            _peer = peer;
            _peerClient = peerClient;
            _peerSettings = peerSettings;
            Consensus = consensus;
            _dfsService = dfsService;
            _ledger = ledger;
            _keySigner = keySigner;
            _logger = logger;
            _memPool = memPool;
            _contract = contract;
            _synchronizer = synchronizer;
            _peerRepository = peerRepository;
            _keyApi = keyApi;
            _peerDiscovery = peerDiscovery;

            var privateKey = keySigner.GetPrivateKey(KeyRegistryTypes.DefaultKey);
            _publicKey = keySigner.CryptoContext.GetPublicKeyFromPrivateKey(privateKey);

        }

        public async Task StartSocketsAsync()
        {
            await _peerClient.StartAsync().ConfigureAwait(false);
            await _peer.StartAsync().ConfigureAwait(false);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            _logger.Information("Starting the Catalyst Node");
            var key = await _keyApi.GetKeyAsync("self").ConfigureAwait(false);
            var peerId = key.Id;

            _logger.Information($"***** using PeerId: {peerId} *****");
            _logger.Information($"***** using PublicKey: {_publicKey.Bytes.ToBase58()} *****");
            _logger.Information($"***** using KvmAddress: {_publicKey.ToKvmAddress()} *****");

            await _peerDiscovery?.DiscoveryAsync();

            await _dfsService.StartAsync().ConfigureAwait(false);

            await StartSocketsAsync().ConfigureAwait(false);

            _synchronizer.StartAsync().ConfigureAwait(false);

            _synchronizer.SyncCompleted.Subscribe((x) =>
            {
                Consensus.StartProducing();
            });

            bool exit;

            do
            {
                await Task.Delay(300, ct); //just to get the exit message at the bottom

                _logger.Debug("Type 'exit' to exit, anything else to continue");
                exit = string.Equals(Console.ReadLine(), "exit", StringComparison.OrdinalIgnoreCase);
            } while (!ct.IsCancellationRequested);

            _logger.Debug("Stopping the Catalyst Node");
        }
    }
}
