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
using Catalyst.Core.Consensus;
using Catalyst.Core.Util;
using Catalyst.Protocol;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.UnitTests.Consensus
{
    public class TransactionComparerByFeeTimestampAndHashTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Random _random;

        public TransactionComparerByFeeTimestampAndHashTests(ITestOutputHelper output)
        {
            _output = output;
            _random = new Random();
        }

        [Fact]
        public void Comparer_should_Order_By_Fees_First()
        {
            var transactions = Enumerable.Range(0, 100)
               .Select(i => TransactionHelper.GetTransaction(
                    transactionFees: (ulong) _random.Next(int.MaxValue),
                    timeStamp: _random.Next(int.MaxValue),
                    signature: _random.Next(int.MaxValue).ToString())
                ).ToList();

            var ordered = transactions
               .OrderByDescending(t => t, TransactionComparerByFeeTimestampAndHash.Default)
               .ToArray();

            ordered.Should().BeInDescendingOrder(t => t.TransactionFees);
            ordered.Select(t => t.TimeStamp.ToDateTime()).Should().NotBeAscendingInOrder();
            ordered.Should().NotBeInDescendingOrder(t => t.Signature, SignatureComparer.Default);
        }

        [Fact]
        public void Comparer_should_Order_By_Fees_First_Then_By_TimeStamp()
        {
            var transactions = Enumerable.Range(0, 100)
               .Select(i => TransactionHelper.GetTransaction(
                    transactionFees: (ulong) i % 3,
                    timeStamp: _random.Next(int.MaxValue),
                    signature: _random.Next(int.MaxValue).ToString())
                ).ToList();

            var ordered = transactions
               .OrderByDescending(t => t, TransactionComparerByFeeTimestampAndHash.Default)
               .ToArray();

            ordered.Should().BeInDescendingOrder(t => t.TransactionFees);
            ordered.Select(t => t.TimeStamp.ToDateTime()).Should().NotBeDescendingInOrder();

            Enumerable.Range(0, 3).ToList().ForEach(i =>
                ordered.Where(t => t.TransactionFees == (ulong) i)
                   .Select(t => t.TimeStamp.ToDateTime()).Should().BeInAscendingOrder());
            
            ordered.Should().NotBeInAscendingOrder(t => t.Signature, SignatureComparer.Default);
        }

        [Fact]
        public void Comparer_should_Order_By_Fees_First_Then_By_TimeStamp_Then_By_Signature()
        {
            var transactions = Enumerable.Range(0, 100)
               .Select(i => TransactionHelper.GetTransaction(
                    transactionFees: (ulong) i % 2,
                    timeStamp: i % 3,
                    signature: _random.Next(int.MaxValue).ToString())
                ).ToList();

            var ordered = transactions
               .OrderByDescending(t => t, TransactionComparerByFeeTimestampAndHash.Default)
               .ToArray();

            ordered.Select(s =>
                    s.TransactionFees.ToString() + "|" + s.TimeStamp.ToString() + "|" +
                    s.Signature.SchnorrSignature.ToBase64() + s.Signature.SchnorrComponent.ToBase64())
               .ToList().ForEach(x => _output.WriteLine(x));

            ordered.Should().BeInDescendingOrder(t => t.TransactionFees);

            Enumerable.Range(0, 2).ToList().ForEach(i =>
            {
                ordered
                   .Select(t => t.TransactionFees == (ulong) i ? t.TimeStamp.Seconds : int.MaxValue)
                   .ToArray()
                   .Where(z => z != int.MaxValue)
                   .Should().BeInAscendingOrder();

                Enumerable.Range(0, 3).ToList().ForEach(j =>
                    ordered.Where(t => t.TransactionFees == (ulong) i
                         && t.TimeStamp.ToDateTime() == DateTime.FromOADate(j)).ToArray()
                       .Should().BeInAscendingOrder(t => t.Signature, SignatureComparer.Default));
            });
            
            ordered.Select(s => 
                    s.TransactionFees.ToString() + "|" + s.TimeStamp.ToString() + "|" +
                    s.Signature.SchnorrSignature.ToBase64() + s.Signature.SchnorrComponent.ToBase64())
               .ToList().ForEach(x => _output.WriteLine(x));

            ordered.Should()
               .NotBeInAscendingOrder(t => t.Signature, SignatureComparer.Default);
        }

        [Fact]
        public void SignatureComparer_should_compare_on_SchnorrSignature_and_SchnorrComponent()
        {
            var signatures = Enumerable.Range(0, 30).Select(x =>
            {
                var signature = TransactionHelper.GetTransactionSignature((x % 3).ToString(), x.ToString());
                return signature;
            }).ToArray();

            var ordered = signatures.OrderBy(s => s, SignatureComparer.Default).ToArray();

            ordered.Should().BeInAscendingOrder(s => s.SchnorrSignature, ByteStringComparer.Default);
            ordered.Should().NotBeInAscendingOrder(s => s.SchnorrComponent, ByteStringComparer.Default);

            Enumerable.Range(0, 3).ToList().ForEach(i =>
            {
                ordered.Where(s => s.SchnorrSignature == i.ToString().ToUtf8ByteString()).ToArray()
                   .Should().BeInAscendingOrder(s => s.SchnorrComponent, ByteStringComparer.Default);
            }); 
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
