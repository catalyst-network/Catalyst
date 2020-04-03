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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NUnit.Framework;


namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests
{
    public class TransactionComparerByPriceTimestampAndHashTests
    {
        private TestContext _output;
        private Random _random;

        [SetUp]
        public void Init()
        {
            _output = TestContext.CurrentContext;
            _random = new Random();
        }

        [Test]
        public void Comparer_should_Order_By_GasPrice_First()
        {
            var transactions = Enumerable.Range(0, 100)
               .Select(i => TransactionHelper.GetPublicTransaction(
                    transactionFees: (ulong) _random.Next(int.MaxValue),
                    gasPrice: (ulong) _random.Next(int.MaxValue),
                    timestamp: _random.Next(int.MaxValue),
                    signature: _random.Next(int.MaxValue).ToString()).PublicEntry
                ).ToList();

            var ordered = transactions
               .OrderByDescending(t => t, TransactionComparerByPriceTimestampAndHash.Default)
               .ToArray();

            ordered.Select(o => o.GasPrice.ToUInt256()).Should().BeInDescendingOrder(t => t);
            ordered.Select(t => t.Timestamp.ToDateTime()).Should().NotBeInAscendingOrder();
            ordered.Should().NotBeInDescendingOrder(t => t.Signature.ToByteArray(), ByteUtil.ByteListMinSizeComparer.Default);
        }

        [Test]
        public void Comparer_should_Order_By_GasPrice_First_Then_By_TimeStamp()
        {
            var transactions = Enumerable.Range(0, 100)
               .Select(i => TransactionHelper.GetPublicTransaction(
                    gasPrice: (ulong) i % 3,
                    timestamp: _random.Next(int.MaxValue),
                    signature: _random.Next(int.MaxValue).ToString()).PublicEntry
                ).ToList();

            var ordered = transactions
               .OrderByDescending(t => t, TransactionComparerByPriceTimestampAndHash.Default)
               .ToArray();

            ordered.Select(o => o.GasPrice.ToUInt256()).Should().BeInDescendingOrder(t => t);
            ordered.Select(t => t.Timestamp.ToDateTime()).Should().NotBeDescendingInOrder();

            Enumerable.Range(0, 3).ToList().ForEach(i =>
                ordered.Where(t => t.GasPrice.ToUInt256() == (ulong) i)
                   .Select(t => t.Timestamp.ToDateTime()).Should().BeInAscendingOrder());

            ordered.Should().NotBeInAscendingOrder(t => t.Signature.ToByteArray(), ByteUtil.ByteListMinSizeComparer.Default);
        }

        [Test]
        public void Comparer_should_Order_By_GasPrice_First_Then_By_TimeStamp_Then_By_Signature()
        {
            var transactions = Enumerable.Range(0, 100)
               .Select(i => TransactionHelper.GetPublicTransaction(
                    gasPrice: (ulong) i % 2,
                    timestamp: i % 3,
                    signature: _random.Next(int.MaxValue).ToString()).PublicEntry
                ).ToList();

            var ordered = transactions
               .OrderByDescending(t => t, TransactionComparerByPriceTimestampAndHash.Default)
               .ToArray();

            ordered.Select(s =>
                    s.GasPrice + "|" + s.Timestamp + "|" +
                    s.Signature.RawBytes.ToBase64())
               .ToList().ForEach(x => TestContext.WriteLine(x));

            ordered.Select(o => o.GasPrice.ToUInt256()).Should().BeInDescendingOrder(t => t);

            Enumerable.Range(0, 2).ToList().ForEach(i =>
            {
                ordered
                   .Select(t => t.GasPrice.ToUInt256() == (ulong) i ? t.Timestamp.Seconds : int.MaxValue)
                   .ToArray()
                   .Where(z => z != int.MaxValue)
                   .Should().BeInAscendingOrder();

                Enumerable.Range(0, 3).ToList().ForEach(j =>
                    ordered.Where(t => t.GasPrice.ToUInt256() == (ulong) i
                         && t.Timestamp.ToDateTime() == DateTime.FromOADate(j)).ToArray()
                       .Select(t => t.Signature.ToByteArray())
                       .Should().BeInAscendingOrder(t => t, ByteUtil.ByteListMinSizeComparer.Default));
            });

            ordered.Select(s =>
                    s.GasPrice.ToUInt256() + "|" + s.Timestamp + "|" +
                    s.Signature.RawBytes.ToBase64())
               .ToList().ForEach(x => TestContext.WriteLine(x));

            ordered.Should()
               .NotBeInAscendingOrder(t => t.Signature.ToByteArray(), ByteUtil.ByteListMinSizeComparer.Default);
        }
    }

    public class ByteStringComparer : IComparer<ByteString>
    {
        public int Compare(ByteString x, ByteString y)
        {
            return ByteUtil.ByteListMinSizeComparer.Default.Compare(x.ToByteArray(), y.ToByteArray());
        }

        public static ByteStringComparer Default { get; } = new ByteStringComparer();
    }
}
