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
using System.Linq;
using System.Reactive.Linq;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Mempool.Documents;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Google.Protobuf;
using LibP2P;
using Microsoft.Reactive.Testing;
using Nethermind.Core.Extensions;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Evm;
using Nethermind.Logging;
using Nethermind.Store;
using NSubstitute;
using TheDotNetLeague.MultiFormats.MultiHash;
using Xunit;
using ILogger = Serilog.ILogger;

namespace Catalyst.Core.Modules.Ledger.Tests.IntegrationTests
{
    public class LedgerKvmTests
    {
        private readonly TestScheduler _testScheduler;
        private Ledger _ledger;
        private readonly IAccountRepository _fakeRepository;
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IMempool<TransactionBroadcastDao> _mempool;
        private readonly ILogger _logger;
        private readonly ILedgerSynchroniser _ledgerSynchroniser;
        private readonly IHashProvider _hashProvider;
        private readonly MultiHash _genesisHash;
        private readonly IKvm _kvm;
        private readonly StateProvider _stateProvider;
        private readonly CatalystSpecProvider _specProvider;

        public LedgerKvmTests()
        {
            _testScheduler = new TestScheduler();
            _fakeRepository = Substitute.For<IAccountRepository>();
            _genesisHash = _hashProvider.ComputeUtf8MultiHash("genesis");

            _logger = Substitute.For<ILogger>();
            _mempool = Substitute.For<IMempool<TransactionBroadcastDao>>();
            _deltaHashProvider = Substitute.For<IDeltaHashProvider>();
            _ledgerSynchroniser = Substitute.For<ILedgerSynchroniser>();
            
            _ledgerSynchroniser.DeltaCache.GenesisHash.Returns(_genesisHash);
            
            IDb stateDbDevice = new MemDb();
            IDb codeDbDevice = new MemDb();

            ISnapshotableDb stateDb = new StateDb(stateDbDevice);
            ISnapshotableDb codeDb = new StateDb(codeDbDevice);

            _stateProvider = new StateProvider(stateDb, codeDb, LimboLogs.Instance);
            IStorageProvider storageProvider = new StorageProvider(stateDb, _stateProvider, LimboLogs.Instance);

            IStateUpdateHashProvider stateUpdateHashProvider = new StateUpdateHashProvider();
            _specProvider = new CatalystSpecProvider();
            
            _kvm = new KatVirtualMachine(_stateProvider, storageProvider, stateUpdateHashProvider, _specProvider, LimboLogs.Instance);
        }

        [Fact]
        public void Should_Mutate_State_Through_Kvm()
        {
            var cryptoContext = new FfiWrapper();

            var hash1 = _hashProvider.ComputeUtf8MultiHash("update");
            var updates = new[] {hash1};
            var receiverPublicEntry = cryptoContext.GeneratePrivateKey().GetPublicKey();
            var receiverContractEntry = cryptoContext.GeneratePrivateKey().GetPublicKey();
            var sender = cryptoContext.GeneratePrivateKey().GetPublicKey();

            _ledgerSynchroniser.CacheDeltasBetween(default, default, default)
               .ReturnsForAnyArgs(new Cid[] {hash1, _genesisHash});
            
            _stateProvider.CreateAccount(receiverContractEntry.ToKvmAddress(), UInt256.Zero);
            _stateProvider.CreateAccount(receiverPublicEntry.ToKvmAddress(), UInt256.Zero);
            
            _ledgerSynchroniser.DeltaCache.TryGetOrAddConfirmedDelta(Arg.Any<string>(), out Arg.Any<Delta>())
               .Returns(c =>
                {
                    c[1] = new Delta()
                    {
                        PublicEntries =
                        {
                            new PublicEntry()
                            {
                                Base = new BaseEntry()
                                {
                                    ReceiverPublicKey =
                                        ByteString.FromBase64(Convert.ToBase64String(receiverPublicEntry.Bytes)),
                                    SenderPublicKey =
                                        ByteString.FromBase64(Convert.ToBase64String(sender.Bytes)),
                                },
                                Amount = ByteString.FromBase64(Convert.ToBase64String(new byte[] {1}))
                            }
                        },
                        ContractEntries =
                        {
                            new ContractEntry()
                            {
                                Base = new BaseEntry()
                                {
                                    ReceiverPublicKey =
                                        ByteString.FromBase64(Convert.ToBase64String(receiverContractEntry.Bytes)),
                                    SenderPublicKey =
                                        ByteString.FromBase64(Convert.ToBase64String(sender.Bytes)),
                                },
                                Amount = ByteString.FromBase64(Convert.ToBase64String(new byte[] {2})),
                                Data = ByteString.FromBase64(Convert.ToBase64String(Bytes.FromHexString("0x4600")))
                            }
                        }
                    };
                    return true;
                }, c => false);

            _deltaHashProvider.DeltaHashUpdates.Returns(updates.Select(h => (Cid) h).ToObservable(_testScheduler));

            _ledger = new Ledger(_kvm, _stateProvider, _specProvider, _fakeRepository, _deltaHashProvider, _ledgerSynchroniser, _mempool, _logger);

            _testScheduler.Start();

            _stateProvider.GetAccount(receiverPublicEntry.ToKvmAddress()).Balance.Should().Be(1);
            _stateProvider.GetAccount(receiverContractEntry.ToKvmAddress()).Balance.Should().Be(2);
        }
    }
}
