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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Extensions.Protocol.Wire;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using LibP2P;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethermind.Core.Extensions;
using Nethermind.Dirichlet.Numerics;
using NSubstitute;
using Serilog;
using TheDotNetLeague.MultiFormats.MultiBase;
using TheDotNetLeague.MultiFormats.MultiHash;
using Xunit;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.Deltas
{
    public sealed class DeltaBuilderTests
    {
        private const ulong DeltaGasLimit = 8_000_000;
        private readonly IHashProvider _hashProvider;
        private readonly IDeterministicRandomFactory _randomFactory;
        private readonly Random _random;
        private readonly PeerId _producerId;
        private readonly Cid _previousDeltaHash;
        private readonly CoinbaseEntry _zeroCoinbaseEntry;
        private readonly IDeltaCache _cache;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger _logger;
        private readonly IPeerSettings _peerSettings;

        public DeltaBuilderTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));

            _random = new Random();

            _randomFactory = Substitute.For<IDeterministicRandomFactory>();
            _randomFactory.GetDeterministicRandomFromSeed(Arg.Any<byte[]>())
               .Returns(ci => new IsaacRandom(((byte[])ci[0]).ToHex()));

            _producerId = PeerIdHelper.GetPeerId("producer");
            _peerSettings = _producerId.ToSubstitutedPeerSettings();

            _previousDeltaHash = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("previousDelta"));
            _zeroCoinbaseEntry = new CoinbaseEntry
            {
                Amount = UInt256.Zero.ToUint256ByteString(),
                ReceiverPublicKey = _producerId.PublicKey.ToByteString()
            };

            _logger = Substitute.For<ILogger>();

            _cache = Substitute.For<IDeltaCache>();

            _dateTimeProvider = new DateTimeProvider();
        }

        [Fact]
        public void BuildDeltaEmptyPoolContent()
        {
            var transactionRetriever = Substitute.For<IDeltaTransactionRetriever>();
            transactionRetriever.GetMempoolTransactionsByPriority()
               .Returns(new List<TransactionBroadcast>());

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashProvider, _peerSettings,
                _cache, _dateTimeProvider, _logger);

            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, _zeroCoinbaseEntry.ToByteArray());

            _cache.Received(1).AddLocalDelta(Arg.Is(candidate), Arg.Any<Delta>());
        }

        [Fact]
        public void BuildDeltaInvalidTransactionsBasedOnLockTime()
        {
            var invalidTransactionList = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetPublicTransaction(
                    transactionFees: 954,
                    timestamp: 157,
                    signature: i.ToString());
                transaction.ConfidentialEntries.Add(new ConfidentialEntry
                {
                    Base = new BaseEntry
                    {
                        ReceiverPublicKey = "this entry makes the transaction invalid".ToUtf8ByteString()
                    }
                });
                return transaction;
            }).ToList();

            var transactionRetriever = Substitute.For<IDeltaTransactionRetriever>();
            transactionRetriever.GetMempoolTransactionsByPriority().Returns(invalidTransactionList);

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashProvider, _peerSettings,
                _cache, _dateTimeProvider, _logger);
            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, _zeroCoinbaseEntry.ToByteArray());

            _cache.Received(1).AddLocalDelta(Arg.Is(candidate), Arg.Any<Delta>());
        }

        [Fact]
        public void BuildDeltaCheckForAccuracy()
        {
            var transactions = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetPublicTransaction(
                    (uint)i,
                    receiverPublicKey: i.ToString(),
                    transactionFees: (ulong)_random.Next(),
                    timestamp: _random.Next(),
                    signature: i.ToString());
                return transaction;
            }).ToList();

            var transactionRetriever = BuildRetriever(transactions);
            var selectedTransactions = BuildSelectedTransactions(transactions);

            var salt = BitConverter.GetBytes(_randomFactory.GetDeterministicRandomFromSeed(_previousDeltaHash.ToArray()).NextInt());

            var rawAndSaltedEntriesBySignature = selectedTransactions.SelectMany(
                t => t.PublicEntries.Select(e =>
                {
                    var publicEntriesProtoBuff = e;
                    return new
                    {
                        RawEntry = publicEntriesProtoBuff,
                        SaltedAndHashedEntry =
                            _hashProvider.ComputeMultiHash(publicEntriesProtoBuff.ToByteArray().Concat(salt))
                    };
                }));

            var shuffledEntriesBytes = rawAndSaltedEntriesBySignature
               .OrderBy(v => v.SaltedAndHashedEntry.ToArray(), ByteUtil.ByteListComparer.Default)
               .SelectMany(v => v.RawEntry.ToByteArray())
               .ToArray();

            var expectedBytesToHash = BuildExpectedBytesToHash(selectedTransactions, shuffledEntriesBytes);

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashProvider, _peerSettings,
                _cache, _dateTimeProvider, _logger);
            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, expectedBytesToHash);

            _cache.Received(1).AddLocalDelta(Arg.Is(candidate), Arg.Any<Delta>());
        }

        private static IDeltaTransactionRetriever BuildRetriever(List<TransactionBroadcast> transactions)
        {
            transactions.ForEach(t => t.AfterConstruction());
            var transactionRetriever = Substitute.For<IDeltaTransactionRetriever>();
            transactionRetriever.GetMempoolTransactionsByPriority().Returns(transactions);
            return transactionRetriever;
        }

        [Fact]
        public void BuildDeltaWithContractEntries()
        {
            var transactions = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetContractTransaction(ByteString.Empty,
                    UInt256.Zero,
                    21000,
                    (20 + i).GFul(),
                    Bytes.Empty,
                    receiverPublicKey: i.ToString(),
                    transactionFees: (ulong)_random.Next(),
                    timestamp: _random.Next(),
                    signature: i.ToString());
                return transaction;
            }).ToList();

            var transactionRetriever = BuildRetriever(transactions);
            var selectedTransactions = BuildSelectedTransactions(transactions);

            var salt = BitConverter.GetBytes(
                _randomFactory.GetDeterministicRandomFromSeed(_previousDeltaHash.ToArray()).NextInt());

            var rawAndSaltedEntriesBySignature = selectedTransactions.SelectMany(
                t => t.PublicEntries.Select(e =>
                {
                    var contractEntriesProtoBuff = e;
                    return new
                    {
                        RawEntry = contractEntriesProtoBuff,
                        SaltedAndHashedEntry =
                            _hashProvider.ComputeMultiHash(contractEntriesProtoBuff.ToByteArray().Concat(salt))
                    };
                }));

            var shuffledEntriesBytes = rawAndSaltedEntriesBySignature
               .OrderBy(v => v.SaltedAndHashedEntry.ToArray(), ByteUtil.ByteListComparer.Default)
               .SelectMany(v => v.RawEntry.ToByteArray())
               .ToArray();

            var expectedBytesToHash = BuildExpectedBytesToHash(selectedTransactions, shuffledEntriesBytes);

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashProvider, _peerSettings,
                _cache, _dateTimeProvider, _logger);
            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, expectedBytesToHash);

            _cache.Received(1).AddLocalDelta(Arg.Is(candidate), Arg.Any<Delta>());
        }

        [Fact]
        public void When_contract_entries_exceed_delta_gas_limit_some_entries_are_ignored()
        {
            // each entry at 1 million gas
            // so only 8 entries allowed
            var transactions = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetContractTransaction(ByteString.Empty,
                    UInt256.Zero,
                    i > 10 ? (uint) DeltaGasLimit / 8U - 10000U : 70000U, // to test scenarios when both single transaction is ignored and all remaining
                    (20 + i).GFul(),
                    Bytes.Empty,
                    receiverPublicKey: i.ToString(),
                    transactionFees: (ulong)_random.Next(),
                    timestamp: _random.Next(),
                    signature: i.ToString());
                return transaction;
            }).ToList();

            var transactionRetriever = BuildRetriever(transactions);
            var expectedSelectedTransactions = BuildSelectedTransactions(transactions.Skip(10).Take(1).Union(transactions.Skip(12).Take(8)).ToList());

            var salt = BitConverter.GetBytes(
                _randomFactory.GetDeterministicRandomFromSeed(_previousDeltaHash.ToArray()).NextInt());

            var rawAndSaltedEntriesBySignature = expectedSelectedTransactions.SelectMany(
                t => t.PublicEntries.Select(e =>
                {
                    var contractEntriesProtoBuff = e;
                    return new
                    {
                        RawEntry = contractEntriesProtoBuff,
                        SaltedAndHashedEntry =
                            _hashProvider.ComputeMultiHash(contractEntriesProtoBuff.ToByteArray().Concat(salt))
                    };
                })).ToArray();

            var shuffledEntriesBytes = rawAndSaltedEntriesBySignature
               .OrderBy(v => v.SaltedAndHashedEntry.ToArray(), ByteUtil.ByteListComparer.Default)
               .SelectMany(v => v.RawEntry.ToByteArray())
               .ToArray();

            var expectedBytesToHash = BuildExpectedBytesToHash(expectedSelectedTransactions, shuffledEntriesBytes);

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashProvider, _peerSettings,
                _cache, _dateTimeProvider, _logger);
            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, expectedBytesToHash);

            _cache.Received(1).AddLocalDelta(Arg.Is(candidate), Arg.Any<Delta>());
        }

        private static TransactionBroadcast[] BuildSelectedTransactions(List<TransactionBroadcast> transactions)
        {
            var selectedTransactions = transactions.Where(t => (t.IsPublicTransaction || t.IsContractCall || t.IsContractDeployment) && t.HasValidEntries()).ToArray();
            return selectedTransactions;
        }

        private byte[] BuildExpectedBytesToHash(TransactionBroadcast[] selectedTransactions, byte[] shuffledEntriesBytes)
        {
            var signaturesInOrder = selectedTransactions
               .Select(p => p.Signature.ToByteArray())
               .OrderBy(s => s, ByteUtil.ByteListComparer.Default)
               .SelectMany(b => b)
               .ToArray();

            var expectedCoinBase = new CoinbaseEntry
            {
                Amount = selectedTransactions.Sum(t => t.SummedEntryFees()).ToUint256ByteString(),
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

            var expectedHash = CidHelper.CreateCid(_hashProvider.ComputeMultiHash(expectedBytesToHash));
            candidate.Hash.ToByteArray().Should().BeEquivalentTo(expectedHash.ToArray());
        }
    }
}
