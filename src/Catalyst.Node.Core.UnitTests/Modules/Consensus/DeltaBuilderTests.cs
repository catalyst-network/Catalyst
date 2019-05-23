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
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Google.Protobuf;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus
{
    public sealed class DeltaBuilderTests
    {
        private readonly IMempool _mempool;

        public DeltaBuilderTests()
        {
            var random = new Random();

            _mempool = Substitute.For<IMempool>();
            var transactions = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetTransaction(
                    version: (uint) i,
                    transactionFees: (ulong) random.Next(),
                    timeStamp: (ulong) random.Next(),
                    signature: i.ToString(),
                    lockTime: 0);
                return transaction;
            }).ToList();

            _mempool.GetMemPoolContent().Returns(transactions);
        }

        [Fact]
        public void BuildDeltaEmptyPoolContent()
        {
            var mempool = Substitute.For<IMempool>();
            mempool.GetMemPoolContent().Returns(new List<Transaction>());

            var deltaBuilder = new DeltaBuilder(mempool, PeerIdentifierHelper.GetPeerIdentifier("testvalue"),
                Encoding.UTF8.GetBytes("kUox886YuiZojgogjtgo83pkUox886YuiZ"));

            var deltaEntity = deltaBuilder.BuildDelta();
            deltaEntity.Should().Be(DeltaEntity.Default); 
        }

        [Fact]
        public void BuildDeltaInvalidTransactionsBasedOnLockTime()
        {
            var random = new Random(12);

            var invalidtransactionList = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetTransaction(
                    version: (uint) i,
                    transactionFees: 954,
                    timeStamp: 157,
                    signature: i.ToString(),
                    lockTime: (ulong) random.Next() + 475);
                return transaction;
            }).ToList();

            var mempool = Substitute.For<IMempool>();
            mempool.GetMemPoolContent().Returns(invalidtransactionList);

            var deltaBuilder = new DeltaBuilder(mempool, PeerIdentifierHelper.GetPeerIdentifier("testvalue"),
                Encoding.UTF8.GetBytes("kUox886YuiZojgogjtgo83pkUox886YuiZ"));

            var deltaEntity = deltaBuilder.BuildDelta();
            deltaEntity.Should().Be(DeltaEntity.Default);
        }

        [Fact]
        public void BuildDeltaCheckForAccuracy()
        {
            //Transactions are faked and will always return results
            //Safe to get first
            var transactionSignature = DeltaBuilder.GetValidTransactionsForDelta(_mempool.GetMemPoolContent())
               .First().Signature.ToByteArray();

            var publicKeySeed = "randomseedm";
            var peerId = PeerIdentifierHelper.GetPeerIdentifier(publicKeySeed);
            var previousLedgerStateUpdate = "bkJsrbzIbuWm8EPSjJ2YicTIe5gIfEdfhXJK7dl7ESkjhDWUxkUox886YuiZnhEj3om5AXmWVcXvfzIAw";

            var preLedgerStateUpdateByteArray = Encoding.UTF8.GetBytes(previousLedgerStateUpdate);

            var deltaBuilder = new DeltaBuilder(_mempool, peerId, preLedgerStateUpdateByteArray);
            var deltaEntity = deltaBuilder.BuildDelta();

            deltaEntity.Should().NotBeNull();

            Encoding.UTF8.GetString(deltaEntity.LocalLedgerState).Should().Contain(publicKeySeed);

            deltaEntity.LocalLedgerState.Should().EndWith(deltaEntity.LocalLedgerState.TakeLast(preLedgerStateUpdateByteArray.Length));

            deltaEntity.Delta.Should().EndWith(transactionSignature);

            deltaEntity.DeltaHash.Should().ContainInOrder(Multihash.Encode<BLAKE2B_256>(deltaEntity.Delta));
        }
    }
}
