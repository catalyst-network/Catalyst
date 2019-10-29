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
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Extensions.Protocol.Wire;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.Deltas
{
    public sealed class DeltaTransactionRetrieverTests
    {
        private readonly IList<TransactionBroadcast> _transactions;
        private readonly DeltaTransactionRetriever _transactionRetriever;
        private readonly TestMapperProvider _mapperProvider;

        public DeltaTransactionRetrieverTests()
        {
            _mapperProvider = new TestMapperProvider();

            var random = new Random();

            var mempool = Substitute.For<IMempool<TransactionBroadcastDao>>();
            _transactions = Enumerable.Range(0, 20).Select(i =>
                TransactionHelper.GetPublicTransaction(
                    transactionFees: (ulong) random.Next(),
                    timestamp: random.Next(),
                    signature: i.ToString())
            ).ToList();

            mempool.Repository.GetAll().Returns(_transactions
               .Select(x => x.ToDao<TransactionBroadcast, TransactionBroadcastDao>(_mapperProvider)));

            _transactionRetriever = new DeltaTransactionRetriever(mempool, _mapperProvider,
                TransactionComparerByFeeTimestampAndHash.Default);
        }

        [Fact]
        public void GetMempoolTransactionsByPriority_should_at_most_return_MaxCount()
        {
            var maxCountBelowTotal = _transactions.Count - 1;
            var retrieved = _transactionRetriever.GetMempoolTransactionsByPriority(maxCountBelowTotal);

            retrieved.Count().Should().BeLessOrEqualTo(maxCountBelowTotal);

            var maxCountAboveTotal = _transactions.Count + 1;
            retrieved = _transactionRetriever.GetMempoolTransactionsByPriority(maxCountAboveTotal);
            retrieved.Count.Should().Be(_transactions.Count);

            retrieved = _transactionRetriever.GetMempoolTransactionsByPriority();
            retrieved.Count.Should().Be(_transactions.Count);
        }

        [Fact]
        public void GetMempoolTransactionsByPriority_should_not_accept_zero_or_negative_maxCount()
        {
            new Action(() => _transactionRetriever.GetMempoolTransactionsByPriority(-1))
               .Should().Throw<ArgumentException>();

            new Action(() => _transactionRetriever.GetMempoolTransactionsByPriority(0))
               .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetMempoolTransactionsByPriority_should_return_transactions_in_decreasing_priority_order()
        {
            var maxCount = _transactions.Count / 2;
            var expectedTransactions = _transactions
               .OrderByDescending(t => t, _transactionRetriever.TransactionComparer)
               .Take(maxCount).ToList();

            var retrievedTransactions = _transactionRetriever
               .GetMempoolTransactionsByPriority(maxCount);

            var excludedTransactionCount = _transactions.Count - maxCount;

            var unexpectedTransactions = _transactions.OrderBy(t => t, _transactionRetriever.TransactionComparer)
               .Take(excludedTransactionCount).ToList();

            unexpectedTransactions
               .ForEach(t => retrievedTransactions.Any(r => t.Signature == r.Signature).Should()
                   .BeFalse("No unexpected transactions should have been retrieved"));

            for (var i = 0; i < maxCount; i++)
            {
                retrievedTransactions[i].IsPublicTransaction.Should().Be(expectedTransactions[i].IsPublicTransaction);
                if (i == 0)
                {
                    continue;
                }

                // just a sanity check to make sure that the order is not opposite of what was intended in
                // TransactionComparerByFeeTimestampAndHash
                retrievedTransactions[i - 1].SummedEntryFees().Should()
                   .BeGreaterOrEqualTo(retrievedTransactions[i].SummedEntryFees());
            }
        }
    }
}
