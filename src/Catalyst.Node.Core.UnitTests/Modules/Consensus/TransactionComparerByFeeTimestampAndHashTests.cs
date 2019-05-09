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
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Consensus;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Modules.Consensus
{
    public class TransactionComparerByFeeTimestampAndHashTests
    {
        private readonly Random _random;

        public TransactionComparerByFeeTimestampAndHashTests()
        {
            _random = new Random();
        }

        [Fact]
        public void Comparer_should_Order_By_Fees_First()
        {
            var transactions = Enumerable.Range(0, 100)
               .Select(i => TransactionHelper.GetTransaction(
                    transactionFees: (ulong) _random.Next(int.MaxValue),
                    timeStamp: (ulong) _random.Next(int.MaxValue),
                    signature: _random.Next(int.MaxValue).ToString())
                ).ToList();

            var ordered = transactions
               .OrderByDescending(t => t, TransactionComparerByFeeTimestampAndHash.Default);

            ordered.Should().BeInDescendingOrder(t => t.TransactionFees);
            ordered.Select(t => t.TimeStamp).Should().NotBeDescendingInOrder();
            ordered.Should().NotBeInDescendingOrder(t => t.Signature, SignatureComparer.Default);
        }

        [Fact]
        public void Comparer_should_Order_By_Fees_First_Then_By_TimeStamp()
        {
            var transactions = Enumerable.Range(0, 100)
               .Select(i => TransactionHelper.GetTransaction(
                    transactionFees: (ulong) i % 3,
                    timeStamp: (ulong) _random.Next(int.MaxValue),
                    signature: _random.Next(int.MaxValue).ToString())
                ).ToList();

            var ordered = transactions
               .OrderByDescending(t => t, TransactionComparerByFeeTimestampAndHash.Default);

            ordered.Should().BeInDescendingOrder(t => t.TransactionFees);
            ordered.Select(t => t.TimeStamp).Should().NotBeDescendingInOrder();

            Enumerable.Range(0, 3).ToList().ForEach(i =>
                ordered.Where(t => t.TransactionFees == (ulong) i).Select(t => t.TimeStamp).Should().BeInDescendingOrder());
            
            ordered.Should().NotBeInDescendingOrder(t => t.Signature, SignatureComparer.Default);
        }

        [Fact]
        public void Comparer_should_Order_By_Fees_First_Then_By_TimeStamp_Then_By_Signature()
        {
            var transactions = Enumerable.Range(0, 100)
               .Select(i => TransactionHelper.GetTransaction(
                    transactionFees: 1,
                    timeStamp: (ulong) i % 3,
                    signature: _random.Next(int.MaxValue).ToString())
                ).ToList();

            var ordered = transactions
               .OrderByDescending(t => t, TransactionComparerByFeeTimestampAndHash.Default);

            ordered.Select(t => t.TimeStamp).Should().BeInDescendingOrder();

            Enumerable.Range(0, 3).ToList().ForEach(i =>
                ordered.Where(t => t.TimeStamp == (ulong) i).ToArray()
                   .Should().BeInDescendingOrder(t => t.Signature, SignatureComparer.Default));

            ordered.Should().NotBeInDescendingOrder(t => t.Signature, SignatureComparer.Default);
        }
    }
}
