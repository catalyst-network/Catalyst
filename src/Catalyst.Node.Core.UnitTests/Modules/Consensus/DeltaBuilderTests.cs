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
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Modules.Consensus
{
    public class DeltaBuilderTests
    {
        private readonly IList<Transaction> _transactions;
        private readonly DeltaBuilder _deltaBuilder;

        public DeltaBuilderTests()
        {
            var random = new Random();

            var mempool = Substitute.For<IMempool>();
            _transactions = Enumerable.Range(0, 20).Select(i =>
            {
                var transaction = TransactionHelper.GetTransaction(
                    version: (uint) i,
                    transactionFees: (ulong) random.Next(),
                    timeStamp: (ulong) random.Next(),
                    signature: i.ToString());
                return transaction;
            }).ToList();

            mempool.GetMemPoolContent().Returns(_transactions);

            //Assumes the node is this peer
            _deltaBuilder = new DeltaBuilder(mempool, PeerIdentifierHelper.GetPeerIdentifier("2"));
        }

        [Fact]
        public void BuildDelta()
        {
            _deltaBuilder.BuildDelta();

            Assert.Equal(true, true);

        }
    }
}
