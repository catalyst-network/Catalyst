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
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Protocol.Deltas;
using Catalyst.Core.Modules.Consensus.Deltas.Building;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Lib.P2P;
using MultiFormats.Registry;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Db;
using Nethermind.Logging;
using Nethermind.State;
using NSubstitute;
using NUnit.Framework;
using ILogger = Serilog.ILogger;
using Nethermind.Int256;
using Nethermind.Blockchain;
using Nethermind.Trie.Pruning;
using Common.Logging;
using Nethermind.Synchronization.SnapSync;
using Nethermind.Synchronization.StateSync;
using Nethermind.Trie;
using Microsoft.AspNetCore.Routing.Tree;
using Nethermind.Blockchain.FullPruning;
using Nethermind.Synchronization;
using Nethermind.Synchronization.Peers.AllocationStrategies;
using Nethermind.Synchronization.Blocks;
using Nethermind.Consensus.Producers;
using Nethermind.Consensus.Processing;
using Nethermind.Blockchain.Visitors;
using Nethermind.Evm.Tracing.GethStyle;
using Nethermind.Blockchain.Receipts;
using Nethermind.Crypto;
using Nethermind.Specs.Forks;
using Nethermind.Specs;
using Nethermind.Core;
using Catalyst.TestUtils.Repository.TreeBuilder;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.Deltas
{
    public sealed class DeltaBuilderTests
    {
        private const ulong DeltaGasLimit = 8_000_000;
        private IHashProvider _hashProvider;
        private IDeterministicRandomFactory _randomFactory;
        private Random _random;
        private PeerId _producerId;
        private Cid _previousDeltaHash;
        private CoinbaseEntry _zeroCoinbaseEntry;
        private IDeltaCache _cache;
        private IDateTimeProvider _dateTimeProvider;
        private IWorldState _stateProvider;
        private IDeltaExecutor _deltaExecutor;
        private ICryptoContext _cryptoContext;
        private ILogger _logger;
        private IPeerSettings _peerSettings;

        [SetUp]
        public void Init()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));

            _random = new Random(1);

            _randomFactory = Substitute.For<IDeterministicRandomFactory>();
            _randomFactory.GetDeterministicRandomFromSeed(Arg.Any<byte[]>())
               .Returns(ci => new IsaacRandom(((byte[]) ci[0]).ToHex()));

            _producerId = PeerIdHelper.GetPeerId("producer");
            _peerSettings = _producerId.ToSubstitutedPeerSettings();

            _previousDeltaHash = _hashProvider.ComputeUtf8MultiHash("previousDelta").ToCid();
            _zeroCoinbaseEntry = new CoinbaseEntry
            {
                Amount = UInt256.Zero.ToUint256ByteString(),
                ReceiverPublicKey = _producerId.PublicKey.ToByteString()
            };

            _logger = Substitute.For<ILogger>();

            _cache = Substitute.For<IDeltaCache>();

            Delta previousDelta = new Delta();
            previousDelta.StateRoot = ByteString.CopyFrom(Keccak.EmptyTreeHash.Bytes);
            _cache.TryGetOrAddConfirmedDelta(Arg.Any<Cid>(), out Arg.Any<Delta>()).Returns(x =>
            {
                x[1] = previousDelta;
                return true;
            });

            _dateTimeProvider = new DateTimeProvider();

            _cryptoContext = new FfiWrapper();

            GethLikeTxTracer txTracer = new GethLikeTxMemoryTracer(GethTraceOptions.Default);

            NoErrorLimboLogs logManager = NoErrorLimboLogs.Instance;

            IDbProvider dbProvider = TestMemDbProvider.Init();
            IDb codeDb = dbProvider.CodeDb;
            IDb stateDb = dbProvider.StateDb;
            SingleReleaseSpecProvider specProvider =
                new(ConstantinopleFix.Instance, MainnetSpecProvider.Instance.NetworkId, MainnetSpecProvider.Instance.ChainId);

            TrieStore trieStore = new(stateDb, LimboLogs.Instance);
            StateReader stateReader = new(trieStore, codeDb, logManager);
            WorldState stateProvider = new(trieStore, codeDb, logManager);
            Assert.That(stateProvider.StateRoot, Is.Not.EqualTo(Keccak.Zero));
            stateProvider.CreateAccount(new Address(new ValueHash256()), 10000.Ether());
            stateProvider.Commit(specProvider.GenesisSpec);
            stateProvider.CommitTree(0);
            stateProvider.RecalculateStateRoot();

            InMemoryReceiptStorage receiptStorage = new();

            EthereumEcdsa ecdsa = new(specProvider.ChainId, logManager);
            BlockTree tree = Build.A.BlockTree().WithoutSettingHead.TestObject;
            BlockhashProvider blockhashProvider = new(tree, specProvider, stateProvider, LimboLogs.Instance);

            KatVirtualMachine virtualMachine = new KatVirtualMachine(_stateProvider,
                blockhashProvider,
                specProvider,
                new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256")),
                new FfiWrapper(), 
                LimboLogs.Instance);
            _deltaExecutor = new DeltaExecutor(specProvider,
                _stateProvider,
                virtualMachine,
                _cryptoContext,
                _logger);
        }

        private readonly Dictionary<int, IPublicKey> _publicKeys = new Dictionary<int, IPublicKey>();

        private IPublicKey GetPublicKey(int index)
        {
            if (!_publicKeys.ContainsKey(index))
            {
                IPublicKey key = _cryptoContext.GeneratePrivateKey().GetPublicKey();
                _publicKeys[index] = key;
            }

            return _publicKeys[index];
        }

        [Test]
        public void BuildDeltaEmptyPoolContent()
        {
            var transactionRetriever = Substitute.For<IDeltaTransactionRetriever>();
            transactionRetriever.GetMempoolTransactionsByPriority()
               .Returns(new List<PublicEntry>());

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashProvider, _peerSettings,
                _cache, _dateTimeProvider, _stateProvider, _deltaExecutor, _logger);

            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, _zeroCoinbaseEntry.ToByteArray());

            _cache.Received(1).AddLocalDelta(Arg.Is(candidate), Arg.Any<Delta>());
        }

        [Test]
        public void BuildDeltaInvalidTransactionsBasedOnLockTime()
        {
            var invalidTransactionList = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetPublicTransaction(
                    transactionFees: 954,
                    timestamp: 157,
                    signature: i.ToString());
                return transaction.PublicEntry;
            }).ToList();

            var transactionRetriever = Substitute.For<IDeltaTransactionRetriever>();
            transactionRetriever.GetMempoolTransactionsByPriority().Returns(invalidTransactionList);

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashProvider, _peerSettings,
                _cache, _dateTimeProvider, _stateProvider, _deltaExecutor, _logger);
            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, _zeroCoinbaseEntry.ToByteArray());

            _cache.Received(1).AddLocalDelta(Arg.Is(candidate), Arg.Any<Delta>());
        }

        [Test]
        public void BuildDeltaCheckForAccuracy()
        {
            var transactions = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetPublicTransaction(
                    (uint) i,
                    receiverPublicKey: i.ToString(),
                    transactionFees: (ulong) _random.Next(),
                    timestamp: _random.Next(),
                    signature: i.ToString());
                return transaction.PublicEntry;
            }).ToList();

            var transactionRetriever = BuildRetriever(transactions);
            var selectedTransactions = BuildSelectedTransactions(transactions);

            var salt = BitConverter.GetBytes(_randomFactory.GetDeterministicRandomFromSeed(_previousDeltaHash.ToArray())
               .NextInt());

            var rawAndSaltedEntriesBySignature = selectedTransactions.Select(e =>
            {
                var publicEntriesProtoBuff = e;
                return new
                {
                    RawEntry = publicEntriesProtoBuff,
                    SaltedAndHashedEntry =
                        _hashProvider.ComputeMultiHash(publicEntriesProtoBuff.ToByteArray().Concat(salt))
                };
            });

            var shuffledEntriesBytes = rawAndSaltedEntriesBySignature
               .OrderBy(v => v.SaltedAndHashedEntry.ToArray(), ByteUtil.ByteListComparer.Default)
               .SelectMany(v => v.RawEntry.ToByteArray())
               .ToArray();

            var expectedBytesToHash = BuildExpectedBytesToHash(selectedTransactions, shuffledEntriesBytes);

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashProvider, _peerSettings,
                _cache, _dateTimeProvider, _stateProvider, _deltaExecutor, _logger);
            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, expectedBytesToHash);

            _cache.Received(1).AddLocalDelta(Arg.Is(candidate), Arg.Any<Delta>());
        }

        private static IDeltaTransactionRetriever BuildRetriever(IList<PublicEntry> transactions)
        {
            var transactionRetriever = Substitute.For<IDeltaTransactionRetriever>();
            transactionRetriever.GetMempoolTransactionsByPriority().Returns(transactions);
            return transactionRetriever;
        }

        [Test]
        public void BuildDeltaWithContractEntries()
        {
            var transactions = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetContractTransaction(ByteString.Empty,
                    UInt256.Zero,
                    21000,
                    (20 + i).GFul(),
                    null,
                    null,
                    timestamp: _random.Next(),
                    signature: i.ToString());
                return transaction.PublicEntry;
            }).ToList();

            var transactionRetriever = BuildRetriever(transactions);
            var selectedTransactions = BuildSelectedTransactions(transactions);

            var salt = BitConverter.GetBytes(
                _randomFactory.GetDeterministicRandomFromSeed(_previousDeltaHash.ToArray()).NextInt());

            var rawAndSaltedEntriesBySignature = selectedTransactions.Select(e =>
            {
                var contractEntriesProtoBuff = e;
                return new
                {
                    RawEntry = contractEntriesProtoBuff,
                    SaltedAndHashedEntry =
                        _hashProvider.ComputeMultiHash(contractEntriesProtoBuff.ToByteArray().Concat(salt))
                };
            });

            var shuffledEntriesBytes = rawAndSaltedEntriesBySignature
               .OrderBy(v => v.SaltedAndHashedEntry.ToArray(), ByteUtil.ByteListComparer.Default)
               .SelectMany(v => v.RawEntry.ToByteArray())
               .ToArray();

            var expectedBytesToHash = BuildExpectedBytesToHash(selectedTransactions, shuffledEntriesBytes);

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashProvider, _peerSettings,
                _cache, _dateTimeProvider, _stateProvider, _deltaExecutor, _logger);
            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, expectedBytesToHash);

            _cache.Received(1).AddLocalDelta(Arg.Is(candidate), Arg.Any<Delta>());
        }

        [Test]
        public void When_contract_entries_exceed_delta_gas_limit_some_entries_are_ignored()
        {
            // each entry at 1 million gas
            // so only 8 entries allowed
            var transactions = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetContractTransaction(ByteString.Empty,
                    UInt256.Zero,
                    i > 10
                        ? (uint) DeltaGasLimit / 8U - 10000U
                        : 70000U, // to test scenarios when both single transaction is ignored and all remaining
                    (20 + i).GFul(),
                    null,
                    null,
                    timestamp: _random.Next(),
                    signature: i.ToString());
                return transaction.PublicEntry;
            }).ToList();

            var transactionRetriever = BuildRetriever(transactions);
            var expectedSelectedTransactions =
                BuildSelectedTransactions(transactions.Skip(10).Take(1).Union(transactions.Skip(12).Take(8)).ToList());

            var salt = BitConverter.GetBytes(
                _randomFactory.GetDeterministicRandomFromSeed(_previousDeltaHash.ToArray()).NextInt());

            var rawAndSaltedEntriesBySignature = expectedSelectedTransactions.Select(e =>
            {
                var contractEntriesProtoBuff = e;
                return new
                {
                    RawEntry = contractEntriesProtoBuff,
                    SaltedAndHashedEntry =
                        _hashProvider.ComputeMultiHash(contractEntriesProtoBuff.ToByteArray().Concat(salt))
                };
            }).ToArray();

            var shuffledEntriesBytes = rawAndSaltedEntriesBySignature
               .OrderBy(v => v.SaltedAndHashedEntry.ToArray(), ByteUtil.ByteListComparer.Default)
               .SelectMany(v => v.RawEntry.ToByteArray())
               .ToArray();

            var expectedBytesToHash = BuildExpectedBytesToHash(expectedSelectedTransactions, shuffledEntriesBytes);

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashProvider, _peerSettings,
                _cache, _dateTimeProvider, _stateProvider, _deltaExecutor, _logger);
            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, expectedBytesToHash);

            _cache.Received(1).AddLocalDelta(Arg.Is(candidate), Arg.Any<Delta>());
        }

        private static PublicEntry[] BuildSelectedTransactions(IEnumerable<PublicEntry> transactions)
        {
            return transactions.Where(t => t.IsPublicTransaction || t.IsContractCall || t.IsContractDeployment)
               .ToArray();
        }

        private byte[] BuildExpectedBytesToHash(PublicEntry[] selectedTransactions, byte[] shuffledEntriesBytes)
        {
            var signaturesInOrder = selectedTransactions
               .Select(p => p.Signature.ToByteArray())
               .OrderBy(s => s, ByteUtil.ByteListComparer.Default)
               .SelectMany(b => b)
               .ToArray();

            var expectedCoinBase = new CoinbaseEntry
            {
                Amount = selectedTransactions.Sum(t => t.GasPrice.ToUInt256() * t.GasLimit).ToUint256ByteString(),
                ReceiverPublicKey = _producerId.PublicKey.ToByteString()
            };

            var expectedBytesToHash = shuffledEntriesBytes.Concat(signaturesInOrder)
               .Concat(expectedCoinBase.ToByteArray()).ToArray();
            return expectedBytesToHash;
        }

        private void ValidateDeltaCandidate(CandidateDeltaBroadcast candidate, byte[] expectedBytesToHash)
        {
            candidate.Should().NotBeNull();
            candidate.ProducerId.Should().Be(_producerId);
            candidate.PreviousDeltaDfsHash.ToByteArray().SequenceEqual(_previousDeltaHash.ToArray()).Should().BeTrue();

            var expectedHash = _hashProvider.ComputeMultiHash(expectedBytesToHash).ToCid();
            candidate.Hash.ToByteArray().Should().BeEquivalentTo(expectedHash.ToArray());
        }
    }
}
