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
using System.Collections.Generic;
using System.Reactive.Linq;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.TestUtils;
using Microsoft.Reactive.Testing;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Store;
using NSubstitute;
using Serilog;
using TheDotNetLeague.MultiFormats.MultiHash;
using Xunit;
using Account = Catalyst.Core.Modules.Ledger.Models.Account;
using LedgerService = Catalyst.Core.Modules.Ledger.Ledger;

namespace Catalyst.Core.Modules.Ledger.Tests.UnitTests
{
    public sealed class LedgerTests : IDisposable
    {
        private readonly TestScheduler _testScheduler;
        private LedgerService _ledger;
        private readonly IAccountRepository _fakeRepository;
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IMempool<TransactionBroadcastDao> _mempool;
        private readonly ILogger _logger;
        private readonly ILedgerSynchroniser _ledgerSynchroniser;
        private readonly IHashProvider _hashProvider;
        private readonly MultiHash _genesisHash;
        private readonly IDeltaExecutor _executor;
        private readonly IStateProvider _stateProvider;
        private readonly IStorageProvider _storageProvider;

        public LedgerTests()
        {
            _testScheduler = new TestScheduler();
            _fakeRepository = Substitute.For<IAccountRepository>();
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));

            _logger = Substitute.For<ILogger>();
            _mempool = Substitute.For<IMempool<TransactionBroadcastDao>>();
            _deltaHashProvider = Substitute.For<IDeltaHashProvider>();
            _ledgerSynchroniser = Substitute.For<ILedgerSynchroniser>();
            _genesisHash = _hashProvider.ComputeUtf8MultiHash("genesis");
            _ledgerSynchroniser.DeltaCache.GenesisHash.Returns(_genesisHash);
            _executor = Substitute.For<IDeltaExecutor>();
            _stateProvider = Substitute.For<IStateProvider>();
            _storageProvider = Substitute.For<IStorageProvider>();
        }

        [Fact]
        public void Save_Account_State_To_Ledger_Repository()
        {
            _ledger = new LedgerService(_executor, _stateProvider, _storageProvider, new StateDb(), new StateDb(), _fakeRepository, _deltaHashProvider, _ledgerSynchroniser, _mempool, _logger);
            const int numAccounts = 10;
            for (var i = 0; i < numAccounts; i++)
            {
                var account = AccountHelper.GetAccount((UInt256)i * 5);
                _ledger.SaveAccountState(account);
            }

            _fakeRepository.Received(10).Add(Arg.Any<Account>());
        }

        [Fact]
        public void Should_Reconcile_On_New_Delta_Hash()
        {
            var hash1 = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("update"));
            var hash2 = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("update again"));
            var updates = new[] { hash1, hash2 };

            _ledgerSynchroniser.CacheDeltasBetween(default, default, default)
               .ReturnsForAnyArgs(new[] { hash2, hash1, _genesisHash });

            _deltaHashProvider.DeltaHashUpdates.Returns(updates.ToObservable(_testScheduler));

            _ledger = new LedgerService(_executor, _stateProvider, _storageProvider, new StateDb(), new StateDb(), _fakeRepository, _deltaHashProvider, _ledgerSynchroniser, _mempool, _logger);

            _testScheduler.Start();

            _mempool.Repository.ReceivedWithAnyArgs(updates.Length).Delete(Arg.Any<IEnumerable<TransactionBroadcastDao>>());
        }

        public void Dispose()
        {
            _ledger?.Dispose();
            _fakeRepository?.Dispose();
        }
    }
}
