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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Extensions.Protocol.Wire;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Cryptography.BulletProofs.Types;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Network;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Lib.P2P;
using Microsoft.Reactive.Testing;
using MultiFormats;
using MultiFormats.Registry;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Evm.Tracing;
using Nethermind.Logging;
using Nethermind.Store;
using NSubstitute;
using SharpRepository.InMemoryRepository;
using Xunit;
using ILogger = Serilog.ILogger;

namespace Catalyst.Core.Modules.Ledger.Tests.IntegrationTests
{
    public sealed class LedgerKvmTests
    {
        private readonly ILogger _logger;
        private readonly MultiHash _genesisHash;
        private readonly IHashProvider _hashProvider;
        private readonly IMapperProvider _mapperProvider;
        private readonly ISpecProvider _specProvider;
        private readonly TestScheduler _testScheduler;
        private readonly StateProvider _stateProvider;
        private readonly ICryptoContext _cryptoContext;
        private readonly IAccountRepository _fakeRepository;
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly ISynchroniser _synchroniser;
        private readonly IMempool<PublicEntryDao> _mempool;
        private readonly IDeltaExecutor _deltaExecutor;
        private readonly IStorageProvider _storageProvider;
        private readonly ISnapshotableDb _stateDb;
        private readonly ISnapshotableDb _codeDb;
        private readonly IDeltaByNumberRepository _deltaByNumber;
        private readonly IPrivateKey _senderPrivateKey;
        private readonly IPublicKey _senderPublicKey;
        private readonly SigningContext _signingContext;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly ITransactionRepository _receipts;
        private Address _senderAddress;

        public LedgerKvmTests()
        {
            _testScheduler = new TestScheduler();
            _cryptoContext = new FfiWrapper();
            _fakeRepository = Substitute.For<IAccountRepository>();
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            _mapperProvider = new TestMapperProvider();
            _genesisHash = _hashProvider.ComputeUtf8MultiHash("genesis");

            _logger = Substitute.For<ILogger>();
            _mempool = Substitute.For<IMempool<PublicEntryDao>>();
            _deltaHashProvider = Substitute.For<IDeltaHashProvider>();
            _synchroniser = Substitute.For<ISynchroniser>();
            _deltaByNumber = Substitute.For<IDeltaByNumberRepository>();
            _receipts = Substitute.For<ITransactionRepository>();

            _synchroniser.DeltaCache.GenesisHash.Returns(_genesisHash);

            var stateDbDevice = new MemDb();
            var codeDbDevice = new MemDb();

            _stateDb = new StateDb(stateDbDevice);
            _codeDb = new StateDb(codeDbDevice);

            _stateProvider = new StateProvider(_stateDb, _codeDb, LimboLogs.Instance);
            _storageProvider = new StorageProvider(_stateDb, _stateProvider, LimboLogs.Instance);

            var stateUpdateHashProvider = new StateUpdateHashProvider();
            _specProvider = new CatalystSpecProvider();

            var kvm = new KatVirtualMachine(_stateProvider, _storageProvider, stateUpdateHashProvider, _specProvider, _hashProvider, _cryptoContext,
                LimboLogs.Instance);
            _deltaExecutor = new DeltaExecutor(_specProvider, _stateProvider, _storageProvider, kvm, new FfiWrapper(),
                _logger);

            _deltaIndexService = new DeltaIndexService(new InMemoryRepository<DeltaIndexDao, string>());
            _senderPrivateKey = _cryptoContext.GetPrivateKeyFromBytes(new byte[32]);
            
            _senderPublicKey = _senderPrivateKey.GetPublicKey();
            _senderAddress = _senderPublicKey.ToKvmAddress();
            
            _signingContext = new SigningContext
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };
        }

        private void RunDeltas(Delta delta)
        {
            var genesisDelta = new Delta
            {
                TimeStamp = Timestamp.FromDateTime(DateTime.UnixEpoch),
                StateRoot = ByteString.CopyFrom(_stateProvider.StateRoot.Bytes),
            };

            MultiHash hash1 = _hashProvider.ComputeUtf8MultiHash("update");
            var updates = new[]
            {
                hash1
            };
            _synchroniser.CacheDeltasBetween(default, default, default)
               .ReturnsForAnyArgs(new Cid[]
                {
                    hash1, _genesisHash
                });

            _synchroniser.DeltaCache.TryGetOrAddConfirmedDelta(Arg.Any<Cid>(), out Arg.Any<Delta>())
               .Returns(c =>
                {
                    delta.PreviousDeltaDfsHash = hash1.ToCid().ToArray().ToByteString(); // lol
                    c[1] = delta;
                    return true;
                }, c =>
                {
                    c[1] = genesisDelta;
                    return true;
                });

            _deltaHashProvider.DeltaHashUpdates.Returns(updates.Select(h => (Cid) h).ToObservable(_testScheduler));

            // do not remove - it registers with observable so there is a reference to this object held until the test is ended
            var _ = new Ledger(_deltaExecutor, _stateProvider, _storageProvider, _stateDb, _codeDb,
                _fakeRepository, _deltaIndexService, _receipts, _deltaHashProvider, _synchroniser, _mempool, _mapperProvider, _hashProvider, _logger);

            _testScheduler.Start();
        }

        [Fact]
        public void Should_Update_State_On_Contract_Entry()
        {
            var recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            _stateProvider.CreateAccount(_senderAddress, 1000);
            _stateProvider.CreateAccount(recipient.ToKvmAddress(), UInt256.Zero);
            _stateProvider.Commit(CatalystGenesisSpec.Instance);
            _stateProvider.CommitTree();

            var delta = new Delta
            {
                StateRoot = _stateProvider.StateRoot.ToByteString(),
                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                PublicEntries =
                {
                    EntryUtils.PrepareContractEntry(recipient.ToKvmAddress(), _senderAddress, 7)
                }
            };
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            RunDeltas(delta);

            var account = _stateProvider.GetAccount(recipient.ToKvmAddress());
            account.Should().NotBe(null);
            account.Balance.Should().Be(7);
        }

        [Fact]
        public void Should_Update_State_On_Public_Entry()
        {
            var recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            _stateProvider.CreateAccount(_senderAddress, 1000);
            _stateProvider.CreateAccount(recipient.ToKvmAddress(), UInt256.Zero);
            _stateProvider.Commit(CatalystGenesisSpec.Instance);
            _stateProvider.CommitTree();

            var delta = new Delta
            {
                StateRoot = _stateProvider.StateRoot.ToByteString(),
                PreviousDeltaDfsHash = ByteString.CopyFrom(_genesisHash.ToArray()),
                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                PublicEntries =
                {
                    EntryUtils.PreparePublicEntry(recipient, _senderPublicKey, 3)
                }
            };
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            RunDeltas(delta);

            var account = _stateProvider.GetAccount(recipient.ToKvmAddress());
            account.Should().NotBe(null);
            account.Balance.Should().Be(3);
        }

        [Fact]
        public void Should_Deploy_Code_On_Code_Entry()
        {
            _stateProvider.CreateAccount(_senderAddress, 1000);
            _stateProvider.Commit(CatalystGenesisSpec.Instance);
            _stateProvider.CommitTree();

            var contractAddress = Address.OfContract(_senderAddress, _stateProvider.GetNonce(_senderAddress));

            // PUSH1 1 PUSH1 0 MSTORE PUSH1 1 PUSH1 31 RETURN STOP
            const string initCodeHex = "0x60016000526001601FF300";

            var delta = new Delta
            {
                StateRoot = _stateProvider.StateRoot.ToByteString(),
                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                PublicEntries =
                {
                    EntryUtils.PrepareContractEntry(null, _senderAddress, 5, initCodeHex)
                }
            };

            var senderAccount = _stateProvider.GetAccount(_senderAddress);
            senderAccount.Should().NotBe(null);
            
            delta.PublicEntries[0].GasLimit =
                1_000_000L; // has to be enough for intrinsic gas 21000 + CREATE + code deposit (~53000)
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);
            RunDeltas(delta);

            var account = _stateProvider.GetAccount(contractAddress);
            account.Should().NotBe(null);

            account.Balance.Should().Be(5);
            _stateProvider.GetCode(account.CodeHash).Should().Equal(Bytes.FromHexString("0x01"));
        }

        [Fact]
        public void Should_Deploy_ERC20()
        {
            _stateProvider.CreateAccount(_senderAddress, 1000);
            _stateProvider.Commit(CatalystGenesisSpec.Instance);
            _stateProvider.CommitTree();
            
            var contractAddress1 = Address.OfContract(_senderAddress,
                _stateProvider.GetNonce(_senderAddress) + 0);
            var contractAddress2 = Address.OfContract(_senderAddress,
                _stateProvider.GetNonce(_senderAddress) + 2);

            const string migrationInitHex =
                "608060405234801561001057600080fd5b50336000806101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506102f8806100606000396000f300608060405260043610610062576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff1680630900f01014610067578063445df0ac146100aa5780638da5cb5b146100d5578063fdacd5761461012c575b600080fd5b34801561007357600080fd5b506100a8600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610159565b005b3480156100b657600080fd5b506100bf610241565b6040518082815260200191505060405180910390f35b3480156100e157600080fd5b506100ea610247565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b34801561013857600080fd5b506101576004803603810190808035906020019092919050505061026c565b005b60008060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff16141561023d578190508073ffffffffffffffffffffffffffffffffffffffff1663fdacd5766001546040518263ffffffff167c010000000000000000000000000000000000000000000000000000000002815260040180828152602001915050600060405180830381600087803b15801561022457600080fd5b505af1158015610238573d6000803e3d6000fd5b505050505b5050565b60015481565b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1681565b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614156102c957806001819055505b505600a165627a7a723058206bf582fa33b86704b115209928731f910b5f9872d5a4c376fe8d639aa373ad790029";
            const string call1Hex = "fdacd5760000000000000000000000000000000000000000000000000000000000000001";
            const string initCodeHex =
                "608060405234801561001057600080fd5b50604051610e30380380610e308339810180604052810190808051906020019092919080518201929190602001805190602001909291908051820192919050505083600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020819055508360008190555082600390805190602001906100b29291906100ee565b5081600460006101000a81548160ff021916908360ff16021790555080600590805190602001906100e49291906100ee565b5050505050610193565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061012f57805160ff191683800117855561015d565b8280016001018555821561015d579182015b8281111561015c578251825591602001919060010190610141565b5b50905061016a919061016e565b5090565b61019091905b8082111561018c576000816000905550600101610174565b5090565b90565b610c8e806101a26000396000f3006080604052600436106100af576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff16806306fdde03146100b4578063095ea7b31461014457806318160ddd146101a957806323b872dd146101d457806327e235e314610259578063313ce567146102b05780635c658165146102e157806370a082311461035857806395d89b41146103af578063a9059cbb1461043f578063dd62ed3e146104a4575b600080fd5b3480156100c057600080fd5b506100c961051b565b6040518080602001828103825283818151815260200191508051906020019080838360005b838110156101095780820151818401526020810190506100ee565b50505050905090810190601f1680156101365780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34801561015057600080fd5b5061018f600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803590602001909291905050506105b9565b604051808215151515815260200191505060405180910390f35b3480156101b557600080fd5b506101be6106ab565b6040518082815260200191505060405180910390f35b3480156101e057600080fd5b5061023f600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803590602001909291905050506106b1565b604051808215151515815260200191505060405180910390f35b34801561026557600080fd5b5061029a600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919050505061094b565b6040518082815260200191505060405180910390f35b3480156102bc57600080fd5b506102c5610963565b604051808260ff1660ff16815260200191505060405180910390f35b3480156102ed57600080fd5b50610342600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610976565b6040518082815260200191505060405180910390f35b34801561036457600080fd5b50610399600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919050505061099b565b6040518082815260200191505060405180910390f35b3480156103bb57600080fd5b506103c46109e4565b6040518080602001828103825283818151815260200191508051906020019080838360005b838110156104045780820151818401526020810190506103e9565b50505050905090810190601f1680156104315780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34801561044b57600080fd5b5061048a600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919080359060200190929190505050610a82565b604051808215151515815260200191505060405180910390f35b3480156104b057600080fd5b50610505600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610bdb565b6040518082815260200191505060405180910390f35b60038054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156105b15780601f10610586576101008083540402835291602001916105b1565b820191906000526020600020905b81548152906001019060200180831161059457829003601f168201915b505050505081565b600081600260003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b925846040518082815260200191505060405180910390a36001905092915050565b60005481565b600080600260008673ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002054905082600160008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002054101580156107825750828110155b151561078d57600080fd5b82600160008673ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828254019250508190555082600160008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825403925050819055507fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff8110156108da5782600260008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825403925050819055505b8373ffffffffffffffffffffffffffffffffffffffff168573ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef856040518082815260200191505060405180910390a360019150509392505050565b60016020528060005260406000206000915090505481565b600460009054906101000a900460ff1681565b6002602052816000526040600020602052806000526040600020600091509150505481565b6000600160008373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020549050919050565b60058054600181600116156101000203166002900480601f016020809104026020016040519081016040528092919081815260200182805460018160011615610100020316600290048015610a7a5780601f10610a4f57610100808354040283529160200191610a7a565b820191906000526020600020905b815481529060010190602001808311610a5d57829003601f168201915b505050505081565b600081600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000205410151515610ad257600080fd5b81600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828254039250508190555081600160008573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825401925050819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef846040518082815260200191505060405180910390a36001905092915050565b6000600260008473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020549050929150505600a165627a7a72305820b01e799233fd04eb73bd6896cd148b8926873dc1418dd3e1a44f25a5e59903d2002900000000000000000000000000000000000000000000000000000000000f42400000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000000800000000000000000000000000000000000000000000000000000000000000c000000000000000000000000000000000000000000000000000000000000000034b4154000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000034b41540000000000000000000000000000000000000000000000000000000000";
            const string call2Hex = "fdacd5760000000000000000000000000000000000000000000000000000000000000001";

            // to // 0x9bced1be5c820ccb4bb52c1f83862d06b6d02c9f

            var delta = new Delta
            {
                StateRoot = _stateProvider.StateRoot.ToByteString(),
                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                PublicEntries =
                {
                    EntryUtils.PrepareContractEntry(null, _senderAddress, 0, migrationInitHex),
                    EntryUtils.PrepareContractEntry(contractAddress1, _senderAddress, 0, call1Hex, 1),
                    EntryUtils.PrepareContractEntry(null, _senderAddress, 0, initCodeHex, 2),
                    EntryUtils.PrepareContractEntry(contractAddress1, _senderAddress, 0, call2Hex, 3)
                }
            };

            delta.PublicEntries[0].GasLimit =
                3_000_000L;                             // has to be enough for intrinsic gas 21000 + CREATE + code deposit
            delta.PublicEntries[1].GasLimit = 100_000L; // has to be enough for intrinsic gas 21000 + call
            delta.PublicEntries[2].GasLimit =
                3_000_000L;                             // has to be enough for intrinsic gas 21000 + CREATE + code deposit
            delta.PublicEntries[3].GasLimit = 100_000L; // has to be enough for intrinsic gas 21000 + call

            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);
            delta.PublicEntries[1].Signature = delta.PublicEntries[1]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);
            delta.PublicEntries[2].Signature = delta.PublicEntries[2]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);
            delta.PublicEntries[3].Signature = delta.PublicEntries[3]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            RunDeltas(delta);

            var migrationAccount = _stateProvider.GetAccount(contractAddress1);
            migrationAccount.Should().NotBe(null);
            migrationAccount.Balance.Should().Be(0);
            _stateProvider.GetCode(migrationAccount.CodeHash).Should().Equal(Bytes.FromHexString(
                "608060405260043610610062576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff1680630900f01014610067578063445df0ac146100aa5780638da5cb5b146100d5578063fdacd5761461012c575b600080fd5b34801561007357600080fd5b506100a8600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610159565b005b3480156100b657600080fd5b506100bf610241565b6040518082815260200191505060405180910390f35b3480156100e157600080fd5b506100ea610247565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b34801561013857600080fd5b506101576004803603810190808035906020019092919050505061026c565b005b60008060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff16141561023d578190508073ffffffffffffffffffffffffffffffffffffffff1663fdacd5766001546040518263ffffffff167c010000000000000000000000000000000000000000000000000000000002815260040180828152602001915050600060405180830381600087803b15801561022457600080fd5b505af1158015610238573d6000803e3d6000fd5b505050505b5050565b60015481565b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1681565b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614156102c957806001819055505b505600a165627a7a723058206bf582fa33b86704b115209928731f910b5f9872d5a4c376fe8d639aa373ad790029"));

            var erc20Account = _stateProvider.GetAccount(contractAddress2);
            erc20Account.Should().NotBe(null);

            erc20Account.Balance.Should().Be(0);
            _stateProvider.GetCode(erc20Account.CodeHash).Should().Equal(Bytes.FromHexString(
                "6080604052600436106100af576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff16806306fdde03146100b4578063095ea7b31461014457806318160ddd146101a957806323b872dd146101d457806327e235e314610259578063313ce567146102b05780635c658165146102e157806370a082311461035857806395d89b41146103af578063a9059cbb1461043f578063dd62ed3e146104a4575b600080fd5b3480156100c057600080fd5b506100c961051b565b6040518080602001828103825283818151815260200191508051906020019080838360005b838110156101095780820151818401526020810190506100ee565b50505050905090810190601f1680156101365780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34801561015057600080fd5b5061018f600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803590602001909291905050506105b9565b604051808215151515815260200191505060405180910390f35b3480156101b557600080fd5b506101be6106ab565b6040518082815260200191505060405180910390f35b3480156101e057600080fd5b5061023f600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803590602001909291905050506106b1565b604051808215151515815260200191505060405180910390f35b34801561026557600080fd5b5061029a600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919050505061094b565b6040518082815260200191505060405180910390f35b3480156102bc57600080fd5b506102c5610963565b604051808260ff1660ff16815260200191505060405180910390f35b3480156102ed57600080fd5b50610342600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610976565b6040518082815260200191505060405180910390f35b34801561036457600080fd5b50610399600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919050505061099b565b6040518082815260200191505060405180910390f35b3480156103bb57600080fd5b506103c46109e4565b6040518080602001828103825283818151815260200191508051906020019080838360005b838110156104045780820151818401526020810190506103e9565b50505050905090810190601f1680156104315780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34801561044b57600080fd5b5061048a600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919080359060200190929190505050610a82565b604051808215151515815260200191505060405180910390f35b3480156104b057600080fd5b50610505600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610bdb565b6040518082815260200191505060405180910390f35b60038054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156105b15780601f10610586576101008083540402835291602001916105b1565b820191906000526020600020905b81548152906001019060200180831161059457829003601f168201915b505050505081565b600081600260003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b925846040518082815260200191505060405180910390a36001905092915050565b60005481565b600080600260008673ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002054905082600160008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002054101580156107825750828110155b151561078d57600080fd5b82600160008673ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828254019250508190555082600160008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825403925050819055507fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff8110156108da5782600260008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825403925050819055505b8373ffffffffffffffffffffffffffffffffffffffff168573ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef856040518082815260200191505060405180910390a360019150509392505050565b60016020528060005260406000206000915090505481565b600460009054906101000a900460ff1681565b6002602052816000526040600020602052806000526040600020600091509150505481565b6000600160008373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020549050919050565b60058054600181600116156101000203166002900480601f016020809104026020016040519081016040528092919081815260200182805460018160011615610100020316600290048015610a7a5780601f10610a4f57610100808354040283529160200191610a7a565b820191906000526020600020905b815481529060010190602001808311610a5d57829003601f168201915b505050505081565b600081600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000205410151515610ad257600080fd5b81600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828254039250508190555081600160008573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825401925050819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef846040518082815260200191505060405180910390a36001905092915050565b6000600260008473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020549050929150505600a165627a7a72305820b01e799233fd04eb73bd6896cd148b8926873dc1418dd3e1a44f25a5e59903d20029"));
        }

        [Fact]
        public void Should_Deploy_ERC20_and_ask_about_balance()
        {
            _stateProvider.CreateAccount(_senderAddress, 1000);
            _stateProvider.Commit(CatalystGenesisSpec.Instance);
            _stateProvider.CommitTree();
            
            var contractAddress1 = Address.OfContract(_senderAddress,
                _stateProvider.GetNonce(_senderAddress));
            var contractAddress2 = Address.OfContract(_senderAddress,
                _stateProvider.GetNonce(_senderAddress) + 2);

            // migration contract
            const string migrationInitHex =
                "608060405234801561001057600080fd5b50336000806101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506102f8806100606000396000f300608060405260043610610062576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff1680630900f01014610067578063445df0ac146100aa5780638da5cb5b146100d5578063fdacd5761461012c575b600080fd5b34801561007357600080fd5b506100a8600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610159565b005b3480156100b657600080fd5b506100bf610241565b6040518082815260200191505060405180910390f35b3480156100e157600080fd5b506100ea610247565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b34801561013857600080fd5b506101576004803603810190808035906020019092919050505061026c565b005b60008060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff16141561023d578190508073ffffffffffffffffffffffffffffffffffffffff1663fdacd5766001546040518263ffffffff167c010000000000000000000000000000000000000000000000000000000002815260040180828152602001915050600060405180830381600087803b15801561022457600080fd5b505af1158015610238573d6000803e3d6000fd5b505050505b5050565b60015481565b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1681565b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614156102c957806001819055505b505600a165627a7a723058206bf582fa33b86704b115209928731f910b5f9872d5a4c376fe8d639aa373ad790029";

            // migration call
            const string call1Hex = "fdacd5760000000000000000000000000000000000000000000000000000000000000001";

            // erc20 contract deployment
            const string initCodeHex =
                "608060405234801561001057600080fd5b50604051610e30380380610e308339810180604052810190808051906020019092919080518201929190602001805190602001909291908051820192919050505083600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020819055508360008190555082600390805190602001906100b29291906100ee565b5081600460006101000a81548160ff021916908360ff16021790555080600590805190602001906100e49291906100ee565b5050505050610193565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061012f57805160ff191683800117855561015d565b8280016001018555821561015d579182015b8281111561015c578251825591602001919060010190610141565b5b50905061016a919061016e565b5090565b61019091905b8082111561018c576000816000905550600101610174565b5090565b90565b610c8e806101a26000396000f3006080604052600436106100af576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff16806306fdde03146100b4578063095ea7b31461014457806318160ddd146101a957806323b872dd146101d457806327e235e314610259578063313ce567146102b05780635c658165146102e157806370a082311461035857806395d89b41146103af578063a9059cbb1461043f578063dd62ed3e146104a4575b600080fd5b3480156100c057600080fd5b506100c961051b565b6040518080602001828103825283818151815260200191508051906020019080838360005b838110156101095780820151818401526020810190506100ee565b50505050905090810190601f1680156101365780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34801561015057600080fd5b5061018f600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803590602001909291905050506105b9565b604051808215151515815260200191505060405180910390f35b3480156101b557600080fd5b506101be6106ab565b6040518082815260200191505060405180910390f35b3480156101e057600080fd5b5061023f600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803590602001909291905050506106b1565b604051808215151515815260200191505060405180910390f35b34801561026557600080fd5b5061029a600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919050505061094b565b6040518082815260200191505060405180910390f35b3480156102bc57600080fd5b506102c5610963565b604051808260ff1660ff16815260200191505060405180910390f35b3480156102ed57600080fd5b50610342600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610976565b6040518082815260200191505060405180910390f35b34801561036457600080fd5b50610399600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919050505061099b565b6040518082815260200191505060405180910390f35b3480156103bb57600080fd5b506103c46109e4565b6040518080602001828103825283818151815260200191508051906020019080838360005b838110156104045780820151818401526020810190506103e9565b50505050905090810190601f1680156104315780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34801561044b57600080fd5b5061048a600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919080359060200190929190505050610a82565b604051808215151515815260200191505060405180910390f35b3480156104b057600080fd5b50610505600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610bdb565b6040518082815260200191505060405180910390f35b60038054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156105b15780601f10610586576101008083540402835291602001916105b1565b820191906000526020600020905b81548152906001019060200180831161059457829003601f168201915b505050505081565b600081600260003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b925846040518082815260200191505060405180910390a36001905092915050565b60005481565b600080600260008673ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002054905082600160008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002054101580156107825750828110155b151561078d57600080fd5b82600160008673ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828254019250508190555082600160008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825403925050819055507fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff8110156108da5782600260008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825403925050819055505b8373ffffffffffffffffffffffffffffffffffffffff168573ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef856040518082815260200191505060405180910390a360019150509392505050565b60016020528060005260406000206000915090505481565b600460009054906101000a900460ff1681565b6002602052816000526040600020602052806000526040600020600091509150505481565b6000600160008373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020549050919050565b60058054600181600116156101000203166002900480601f016020809104026020016040519081016040528092919081815260200182805460018160011615610100020316600290048015610a7a5780601f10610a4f57610100808354040283529160200191610a7a565b820191906000526020600020905b815481529060010190602001808311610a5d57829003601f168201915b505050505081565b600081600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000205410151515610ad257600080fd5b81600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828254039250508190555081600160008573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825401925050819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef846040518082815260200191505060405180910390a36001905092915050565b6000600260008473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020549050929150505600a165627a7a72305820b01e799233fd04eb73bd6896cd148b8926873dc1418dd3e1a44f25a5e59903d2002900000000000000000000000000000000000000000000000000000000000f42400000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000000800000000000000000000000000000000000000000000000000000000000000c000000000000000000000000000000000000000000000000000000000000000034b4154000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000034b41540000000000000000000000000000000000000000000000000000000000";

            // migration call
            const string call2Hex = "fdacd5760000000000000000000000000000000000000000000000000000000000000001";

            const string transfer =
                "a9059cbb000000000000000000000000aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa000000000000000000000000000000000000000000000000000000000007a120";

            // _stateProvider.UpdateRootHash(); after Nethermind pull
            var delta = new Delta
            {
                StateRoot = _stateProvider.StateRoot.ToByteString(),
                PreviousDeltaDfsHash = ByteString.CopyFrom(new byte[32]),
                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                PublicEntries =
                {
                    EntryUtils.PrepareContractEntry(null, _senderAddress, 0, migrationInitHex),
                    EntryUtils.PrepareContractEntry(contractAddress1, _senderAddress, 0, call1Hex, 1),
                    EntryUtils.PrepareContractEntry(null, _senderAddress, 0, initCodeHex, 2),
                    EntryUtils.PrepareContractEntry(contractAddress1, _senderAddress, 0, call2Hex, 3),
                    EntryUtils.PrepareContractEntry(contractAddress2, _senderAddress, 0, transfer, 4)
                }
            };

            delta.PublicEntries[0].GasLimit =
                3_000_000L;                             // has to be enough for intrinsic gas 21000 + CREATE + code deposit
            delta.PublicEntries[1].GasLimit = 100_000L; // has to be enough for intrinsic gas 21000 + call
            delta.PublicEntries[2].GasLimit =
                3_000_000L;                             // has to be enough for intrinsic gas 21000 + CREATE + code deposit
            delta.PublicEntries[3].GasLimit = 100_000L; // has to be enough for intrinsic gas 21000 + call
            delta.PublicEntries[4].GasLimit = 200_000;
            delta.PublicEntries[4].ReceiverAddress = contractAddress2.Bytes.ToByteString();

            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);
            delta.PublicEntries[1].Signature = delta.PublicEntries[1]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);
            delta.PublicEntries[2].Signature = delta.PublicEntries[2]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);
            delta.PublicEntries[3].Signature = delta.PublicEntries[3]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);
            delta.PublicEntries[4].Signature = delta.PublicEntries[4]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            RunDeltas(delta);

            var migrationAccount = _stateProvider.GetAccount(contractAddress1);
            migrationAccount.Should().NotBe(null);
            migrationAccount.Balance.Should().Be(0);
            _stateProvider.GetCode(migrationAccount.CodeHash).Should().Equal(Bytes.FromHexString(
                "608060405260043610610062576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff1680630900f01014610067578063445df0ac146100aa5780638da5cb5b146100d5578063fdacd5761461012c575b600080fd5b34801561007357600080fd5b506100a8600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610159565b005b3480156100b657600080fd5b506100bf610241565b6040518082815260200191505060405180910390f35b3480156100e157600080fd5b506100ea610247565b604051808273ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200191505060405180910390f35b34801561013857600080fd5b506101576004803603810190808035906020019092919050505061026c565b005b60008060009054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff16141561023d578190508073ffffffffffffffffffffffffffffffffffffffff1663fdacd5766001546040518263ffffffff167c010000000000000000000000000000000000000000000000000000000002815260040180828152602001915050600060405180830381600087803b15801561022457600080fd5b505af1158015610238573d6000803e3d6000fd5b505050505b5050565b60015481565b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1681565b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614156102c957806001819055505b505600a165627a7a723058206bf582fa33b86704b115209928731f910b5f9872d5a4c376fe8d639aa373ad790029"));

            var erc20Account = _stateProvider.GetAccount(contractAddress2);
            erc20Account.Balance.Should().Be(0);
            _stateProvider.GetCode(erc20Account.CodeHash).Should().Equal(Bytes.FromHexString(
                "6080604052600436106100af576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff16806306fdde03146100b4578063095ea7b31461014457806318160ddd146101a957806323b872dd146101d457806327e235e314610259578063313ce567146102b05780635c658165146102e157806370a082311461035857806395d89b41146103af578063a9059cbb1461043f578063dd62ed3e146104a4575b600080fd5b3480156100c057600080fd5b506100c961051b565b6040518080602001828103825283818151815260200191508051906020019080838360005b838110156101095780820151818401526020810190506100ee565b50505050905090810190601f1680156101365780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34801561015057600080fd5b5061018f600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803590602001909291905050506105b9565b604051808215151515815260200191505060405180910390f35b3480156101b557600080fd5b506101be6106ab565b6040518082815260200191505060405180910390f35b3480156101e057600080fd5b5061023f600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803590602001909291905050506106b1565b604051808215151515815260200191505060405180910390f35b34801561026557600080fd5b5061029a600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919050505061094b565b6040518082815260200191505060405180910390f35b3480156102bc57600080fd5b506102c5610963565b604051808260ff1660ff16815260200191505060405180910390f35b3480156102ed57600080fd5b50610342600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610976565b6040518082815260200191505060405180910390f35b34801561036457600080fd5b50610399600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919050505061099b565b6040518082815260200191505060405180910390f35b3480156103bb57600080fd5b506103c46109e4565b6040518080602001828103825283818151815260200191508051906020019080838360005b838110156104045780820151818401526020810190506103e9565b50505050905090810190601f1680156104315780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34801561044b57600080fd5b5061048a600480360381019080803573ffffffffffffffffffffffffffffffffffffffff16906020019092919080359060200190929190505050610a82565b604051808215151515815260200191505060405180910390f35b3480156104b057600080fd5b50610505600480360381019080803573ffffffffffffffffffffffffffffffffffffffff169060200190929190803573ffffffffffffffffffffffffffffffffffffffff169060200190929190505050610bdb565b6040518082815260200191505060405180910390f35b60038054600181600116156101000203166002900480601f0160208091040260200160405190810160405280929190818152602001828054600181600116156101000203166002900480156105b15780601f10610586576101008083540402835291602001916105b1565b820191906000526020600020905b81548152906001019060200180831161059457829003601f168201915b505050505081565b600081600260003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b925846040518082815260200191505060405180910390a36001905092915050565b60005481565b600080600260008673ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002054905082600160008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002054101580156107825750828110155b151561078d57600080fd5b82600160008673ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828254019250508190555082600160008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825403925050819055507fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff8110156108da5782600260008773ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825403925050819055505b8373ffffffffffffffffffffffffffffffffffffffff168573ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef856040518082815260200191505060405180910390a360019150509392505050565b60016020528060005260406000206000915090505481565b600460009054906101000a900460ff1681565b6002602052816000526040600020602052806000526040600020600091509150505481565b6000600160008373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020549050919050565b60058054600181600116156101000203166002900480601f016020809104026020016040519081016040528092919081815260200182805460018160011615610100020316600290048015610a7a5780601f10610a4f57610100808354040283529160200191610a7a565b820191906000526020600020905b815481529060010190602001808311610a5d57829003601f168201915b505050505081565b600081600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000205410151515610ad257600080fd5b81600160003373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020016000206000828254039250508190555081600160008573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020600082825401925050819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef846040518082815260200191505060405180910390a36001905092915050565b6000600260008473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff16815260200190815260200160002060008373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff168152602001908152602001600020549050929150505600a165627a7a72305820b01e799233fd04eb73bd6896cd148b8926873dc1418dd3e1a44f25a5e59903d20029"));

            // transfer of 500k to aaa...aaa (only here because of the issues with recipient address for smart contracts)
            _stateProvider.GetAccount(_senderAddress).Nonce.Should().Be(5);

            // need to commit changes as the next two calls are reverting changes on the same state provider
            _storageProvider.Commit();
            _stateProvider.Commit(_specProvider.GetSpec(1));
            _storageProvider.CommitTrees();
            _stateProvider.CommitTree();

            // balance should be 500_000 0x7a120
            var balanceOf1 = "70a08231000000000000000000000000" +
                _senderAddress.ToString(false, false);
            var balanceOf1Tracer = new CallOutputTracer();
            var balanceOf1Delta =
                EntryUtils.PrepareSingleContractEntryDelta(_senderPublicKey, _senderPublicKey, 0, balanceOf1, 5);
            balanceOf1Delta.PublicEntries[0].ReceiverAddress = contractAddress2.Bytes.ToByteString();
            balanceOf1Delta.PublicEntries[0].GasLimit = 200_000;
            balanceOf1Delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);
            _deltaExecutor.CallAndReset(balanceOf1Delta, balanceOf1Tracer);
            balanceOf1Tracer.StatusCode.Should().Be(1);
            balanceOf1Tracer.ReturnValue.Should().Equal(Bytes.FromHexString("0x7a120").PadLeft(32));

            // potential bug in Nethermind
            _stateProvider.GetAccount(_senderAddress).Nonce.Should().Be(6);

            // balance should be 500_000 0x7a120
            const string balanceOf2 = "70a08231000000000000000000000000aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            var balanceOf2Delta =
                EntryUtils.PrepareSingleContractEntryDelta(_senderPublicKey, _senderPublicKey, 0, balanceOf2, 6);
            balanceOf2Delta.PublicEntries[0].GasLimit = 200_000;
            balanceOf2Delta.PublicEntries[0].ReceiverAddress = contractAddress2.Bytes.ToByteString();
            balanceOf2Delta.PublicEntries[0].Signature = balanceOf2Delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);
            var balanceOf2Tracer = new CallOutputTracer();
            _deltaExecutor.CallAndReset(balanceOf2Delta, balanceOf2Tracer);
            balanceOf2Tracer.StatusCode.Should().Be(1);
            balanceOf2Tracer.ReturnValue.Should().Equal(Bytes.FromHexString("0x7a120").PadLeft(32));

            // potential bug in Nethermind
            _stateProvider.GetAccount(_senderAddress).Nonce.Should().Be(7);
        }
    }
}
