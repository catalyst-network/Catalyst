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
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.Modules.Ledger.Models;
using Catalyst.TestUtils;
using Microsoft.Reactive.Testing;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Serilog;
using Xunit;
using LedgerService = Catalyst.Core.Lib.Modules.Ledger.Ledger;

namespace Catalyst.Core.Lib.UnitTests.Modules.Ledger
{
    public sealed class LedgerTests : IDisposable
    {
        private readonly TestScheduler _testScheduler;
        private LedgerService _ledger;
        private readonly IAccountRepository _fakeRepository;
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IMempool _mempool;
        private readonly ILogger _logger;

        public LedgerTests()
        {
            _testScheduler = new TestScheduler();
            _fakeRepository = Substitute.For<IAccountRepository>();

            _logger = Substitute.For<ILogger>();
            _mempool = Substitute.For<IMempool>();
            _deltaHashProvider = Substitute.For<IDeltaHashProvider>();
        }

        [Fact]
        public void Save_Account_State_To_Ledger_Repository()
        {
            _ledger = new LedgerService(_fakeRepository, _deltaHashProvider, _mempool, _logger);
            const int numAccounts = 10;
            for (var i = 0; i < numAccounts; i++)
            {
                var account = AccountHelper.GetAccount(balance: i * 5);
                _ledger.SaveAccountState(account);
            }

            _fakeRepository.Received(10).Add(Arg.Any<Account>());
        }

        [Fact]
        public async Task Should_Reconcile_On_New_Delta_Hash()
        {
            var blake2B256 = new BLAKE2B_256();
            var hash1 = "update".ComputeUtf8Multihash(blake2B256);
            var hash2 = "update again".ComputeUtf8Multihash(blake2B256);
            var updates = new[] {hash1, hash2};

            _deltaHashProvider.DeltaHashUpdates.Returns(updates.ToObservable(_testScheduler));

            _ledger = new LedgerService(_fakeRepository, _deltaHashProvider, _mempool, _logger);

            _testScheduler.Start();
            //await _deltaHashProvider.DeltaHashUpdates.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            _mempool.ReceivedWithAnyArgs(updates.Length).Delete(default);
        }

        public void Dispose()
        {
            _ledger?.Dispose();
            _fakeRepository?.Dispose();
        }
    }
}
