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
using System.Text;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Util;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Protocol.Delta;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Google.Protobuf;
using Ipfs;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Catalyst.Node.Core.UnitTest.Modules.Consensus
{
    public sealed class DeltaBuilderTests
    {
        private readonly IDeterministicRandomFactory _randomFactory;
        private readonly IMultihashAlgorithm _hashAlgorithm;
        private readonly Random _random;
        private readonly IPeerIdentifier _producerId;
        private readonly byte[] _previousDeltaHash;
        private CoinbaseEntry _zeroCoinbaseEntry;

        public DeltaBuilderTests()
        {
            _random = new Random();

            _hashAlgorithm = Substitute.For<IMultihashAlgorithm>();
            _hashAlgorithm.ComputeHash(Arg.Any<byte[]>()).Returns(ci => ((byte[]) ci[0]).Reverse().ToArray());

            _randomFactory = Substitute.For<IDeterministicRandomFactory>();
            _randomFactory.GetDeterministicRandomFromSeed(Arg.Any<byte[]>())
               .Returns(ci => new IsaacRandom(((byte[]) ci[0]).ToHex()));

            _producerId = PeerIdentifierHelper.GetPeerIdentifier("producer");

            _previousDeltaHash = Encoding.UTF8.GetBytes("previousDelta");
            _zeroCoinbaseEntry = new CoinbaseEntry {Amount = 0, PubKey = _producerId.PublicKey.ToByteString(), Version = 1};
        }

        [Fact]
        public void BuildDeltaEmptyPoolContent()
        {
            var transactionRetriever = Substitute.For<IDeltaTransactionRetriever>();
            transactionRetriever.GetMempoolTransactionsByPriority().Returns(new List<Transaction>());
            
            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashAlgorithm, _producerId);

            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, _zeroCoinbaseEntry.ToByteArray());
        }

        [Fact]
        public void BuildDeltaInvalidTransactionsBasedOnLockTime()
        {
            var random = new Random(12);

            var invalidTransactionList = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetTransaction(
                    version: (uint) i,
                    transactionFees: 954,
                    timeStamp: 157,
                    signature: i.ToString(),
                    lockTime: (ulong) random.Next() + 475);
                return transaction;
            }).ToList();

            var transactionRetriever = Substitute.For<IDeltaTransactionRetriever>();
            transactionRetriever.GetMempoolTransactionsByPriority().Returns(invalidTransactionList);

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashAlgorithm, _producerId);
            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, _zeroCoinbaseEntry.ToByteArray());
        }

        [Fact]
        public void BuildDeltaCheckForAccuracy()
        {
            var transactions = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetTransaction(
                    standardAmount: (uint) i,
                    standardPubKey: i.ToString(),
                    version: (uint) i % 2,
                    transactionFees: (ulong) _random.Next(),
                    timeStamp: (ulong) _random.Next(),
                    signature: i.ToString(),
                    lockTime: 0);
                return transaction;
            }).ToList();

            var transactionRetriever = Substitute.For<IDeltaTransactionRetriever>();
            transactionRetriever.GetMempoolTransactionsByPriority().Returns(transactions);

            var selectedTransactions = transactions.Where(t => t.Version == 1).ToArray();

            var expectedCoinBase = new CoinbaseEntry
            {
                Amount = selectedTransactions.Sum(t => t.TransactionFees),
                Version = 1,
                PubKey = _producerId.PublicKey.ToByteString()
            };

            var salt = BitConverter.GetBytes(
                _randomFactory.GetDeterministicRandomFromSeed(_previousDeltaHash).NextInt());

            var rawAndSaltedEntriesBySignature = selectedTransactions.SelectMany(
                t => t.STEntries.Select(e => new
                {
                    RawEntry = e,
                    SaltedAndHashedEntry = _hashAlgorithm.ComputeHash(e.ToByteArray().Concat(salt).ToArray())
                }));

            var shuffledEntriesBytes = rawAndSaltedEntriesBySignature
               .OrderBy(v => v.SaltedAndHashedEntry, ByteUtil.ByteListComparer.Default)
               .SelectMany(v => v.RawEntry.ToByteArray())
               .ToArray();

            var signaturesInOrder = selectedTransactions
               .Select(p => p.Signature.ToByteArray())
               .OrderBy(s => s, ByteUtil.ByteListComparer.Default)
               .SelectMany(b => b)
               .ToArray();

            var expectedBytesToHash = shuffledEntriesBytes.Concat(signaturesInOrder)
               .Concat(expectedCoinBase.ToByteArray()).ToArray();

            var deltaBuilder = new DeltaBuilder(transactionRetriever, _randomFactory, _hashAlgorithm, _producerId);
            var candidate = deltaBuilder.BuildCandidateDelta(_previousDeltaHash);

            ValidateDeltaCandidate(candidate, expectedBytesToHash);
        }

        private void ValidateDeltaCandidate(CandidateDelta candidate, byte[] expectedCandidateHash)
        {
            candidate.Should().NotBeNull();
            candidate.ProducerId.Should().Be(_producerId.PeerId);
            candidate.PreviousDeltaDfsHash.ToByteArray().SequenceEqual(_previousDeltaHash).Should().BeTrue();

            var expectedHash = _hashAlgorithm.ComputeHash(expectedCandidateHash);
            candidate.Hash.SequenceEqual(expectedHash).Should().BeTrue();
        }
    }
}
