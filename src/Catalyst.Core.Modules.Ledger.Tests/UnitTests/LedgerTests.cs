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
using System.Linq;
using System.Reactive.Linq;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Extensions.Protocol.Wire;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Core.Modules.Mempool.Repositories;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using LibP2P;
using Microsoft.Reactive.Testing;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Store;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
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
        private readonly IMempool<PublicEntryDao> _mempool;
        private readonly ILogger _logger;
        private readonly ILedgerSynchroniser _ledgerSynchroniser;
        private readonly IHashProvider _hashProvider;
        private readonly IMapperProvider _mapperProvider;
        private readonly Cid _genesisHash;
        private readonly IDeltaExecutor _executor;
        private readonly IStateProvider _stateProvider;
        private readonly IStorageProvider _storageProvider;
        private readonly ICryptoContext _cryptoContext;
        private readonly SigningContext _signingContext;

        public LedgerTests()
        {
            _testScheduler = new TestScheduler();
            _fakeRepository = Substitute.For<IAccountRepository>();
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            _mapperProvider = new TestMapperProvider();

            _logger = Substitute.For<ILogger>();
            _mempool = Substitute.For<IMempool<PublicEntryDao>>();
            _deltaHashProvider = Substitute.For<IDeltaHashProvider>();
            _ledgerSynchroniser = Substitute.For<ILedgerSynchroniser>();
            _genesisHash = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("genesis"));
            _ledgerSynchroniser.DeltaCache.GenesisHash.Returns(_genesisHash);
            _executor = Substitute.For<IDeltaExecutor>();
            _stateProvider = Substitute.For<IStateProvider>();
            _storageProvider = Substitute.For<IStorageProvider>();
            _cryptoContext = new FfiWrapper();
            _signingContext = new SigningContext
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };
        }

        [Fact]
        public void Save_Account_State_To_Ledger_Repository()
        {
            _ledger = new LedgerService(_executor, _stateProvider, _storageProvider, new StateDb(), new StateDb(), _fakeRepository, _deltaHashProvider, _ledgerSynchroniser, _mempool, _mapperProvider, _logger);
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

            _ledgerSynchroniser.CacheDeltasBetween(Arg.Is(_genesisHash), Arg.Is(hash1), default)
               .ReturnsForAnyArgs(new[] { hash2, hash1, _genesisHash });

            _deltaHashProvider.DeltaHashUpdates.Returns(updates.ToObservable(_testScheduler));

            _ledger = new LedgerService(_executor, _stateProvider, _storageProvider, new StateDb(), new StateDb(), _fakeRepository, _deltaHashProvider, _ledgerSynchroniser, _mempool, _mapperProvider, _logger);

            _testScheduler.Start();

            _ledger.LatestKnownDelta.Should().Be(_genesisHash);
        }

        private IEnumerable<PublicEntry> GenerateSamplePublicTransactions(int sampleSize)
        {
            for (var i = 0; i < sampleSize; i++)
            {
                var sender = _cryptoContext.GeneratePrivateKey();
                var recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
                var publicEntry = EntryUtils.PreparePublicEntry(recipient, sender.GetPublicKey(), 10);
                publicEntry.Signature = publicEntry.GenerateSignature(_cryptoContext, sender, _signingContext);
                yield return publicEntry;
            }
        }

        [Fact]
        public void Should_Delete_MempoolItems_On_New_Delta_Hash()
        {
            var sampleSize = 5;
            _mempool.Service.Returns(new MempoolService(new InMemoryRepository<PublicEntryDao, string>()));

            var hash = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("update"));
            var updates = new[] { hash };

            _ledgerSynchroniser.CacheDeltasBetween(Arg.Is(_genesisHash), Arg.Is(hash), default)
               .ReturnsForAnyArgs(new[] { hash, _genesisHash });

            var allPublicEntries = GenerateSamplePublicTransactions(sampleSize * 2).ToList();

            //Add all public entries to the mempool
            allPublicEntries.Select(x => x.ToDao<PublicEntry, PublicEntryDao>(_mapperProvider)).ToList().ForEach(x => _mempool.Service.CreateItem(x));

            //Only add half of all public entries to the delta
            var delta = new Delta()
            {
                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                PublicEntries = { allPublicEntries.Take(sampleSize) }
            };

            _ledgerSynchroniser.DeltaCache.TryGetOrAddConfirmedDelta(Arg.Is(hash), out Arg.Any<Delta>()).Returns(x => { x[1] = delta; return true; });

            _deltaHashProvider.DeltaHashUpdates.Returns(updates.ToObservable(_testScheduler));

            _ledger = new LedgerService(_executor, _stateProvider, _storageProvider, new StateDb(), new StateDb(), _fakeRepository, _deltaHashProvider, _ledgerSynchroniser, _mempool, _mapperProvider, _logger);

            _testScheduler.Start();

            _mempool.Service.GetAll().Should().HaveCount(sampleSize);
        }

        public void Dispose()
        {
            _ledger?.Dispose();
            _fakeRepository?.Dispose();
        }
    }
}
