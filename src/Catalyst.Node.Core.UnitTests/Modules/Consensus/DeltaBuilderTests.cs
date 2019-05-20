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
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Catalyst.Common.Util;
using Catalyst.Common.Extensions;


namespace Catalyst.Node.Core.UnitTest.Modules.Consensus
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

            var deltaBuilder = new DeltaBuilder(mempool, PeerIdentifierHelper.GetPeerIdentifier("testvalue"), ("kUox886YuiZojgogjtgo83pkUox886YuiZ").ToUtf8ByteString().ToArray());

            var deltaEntity = deltaBuilder.BuildDelta();
            deltaEntity.Should().BeNull();
        }

        [Fact]
        public void BuildDeltaInvalidTransactionsBasedOnLockTime()
        {
            var random = new Random(12);

            var invalidtransactionList = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetTransaction(
                    version: (uint)i,
                    transactionFees: (ulong)954,
                    timeStamp: (ulong)157,
                    signature: i.ToString(),
                    lockTime: (ulong)random.Next() + 475);
                return transaction;
            }).ToList();

            var mempool = Substitute.For<IMempool>();
            mempool.GetMemPoolContent().Returns(invalidtransactionList);

            var deltaBuilder = new DeltaBuilder(mempool, PeerIdentifierHelper.GetPeerIdentifier("testvalue"), ("kUox886YuiZojgogjtgo83pkUox886YuiZ").ToUtf8ByteString().ToArray());

            var deltaEntity = deltaBuilder.BuildDelta();
            deltaEntity.Should().BeNull();
        }

        [Theory]
        [InlineData("randomseedm", "bkJsrbzIbuWm8EPSjJ2YicTIe5gIfEdfhXJK7dl7ESkjhDWUxkUox886YuiZnhEj3om5AXmWVcXvfzIAw")]
        public void BuildDeltaSpecifiedValues(string seed, string previousLedgerStateUpdate)
        {
            var deltaBuilder = new DeltaBuilder(_mempool, PeerIdentifierHelper.GetPeerIdentifier(seed), previousLedgerStateUpdate.ToUtf8ByteString().ToArray());

            var deltaEntity = deltaBuilder.BuildDelta();

            deltaEntity.Should().NotBeNull();
            deltaEntity.LocalLedgerState.ToByteString().ToStringUtf8().Should().Be("��\u0002!\n\u000estandardPubKey\u0010�\u0001\n\u00011\u0012\tchallenge\n\u0002Tc\u0012\u000201\u001a\u0010\0\0\0\0\0\0\0\0\0\0\0\0\u007f\0\0\u0001\"\u000290*\u0014randomseedm\0\0\0\0\0\0\0\0\0");
            deltaEntity.Delta.ToByteString().ToStringUtf8().Should().Be("\n\u000estandardPubKey\u0010�\u0001\n\u00011\u0012\tchallenge");
            deltaEntity.DeltaHash.ToByteString().ToStringUtf8().Should().Be("��\u0002!\n\u000estandardPubKey\u0010�\u0001\n\u00011\u0012\tchallenge");
        }
    }
}
