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
using Catalyst.Common.Util;
using FluentAssertions;
using Google.Protobuf;
using Xunit;

namespace Catalyst.Common.UnitTests.Utils
{
    public sealed class ByteUtilTests
    {
        [Theory]
        [InlineData(2)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(10000)]
        [InlineData(5000000)]
        public void Convert_ByteArray_To_ByteString_Should_Succeed(int arraySize)
        {
            var testBytes = GeneratePopulatedBytesArray(arraySize);

            ByteUtil.ToByteString(testBytes).Should().Equal(ByteString.CopyFrom(testBytes));
        }

        [Fact]
        public void Convert_ByteArray_To_ByteString_Should_Fail()
        {
            var testBytes = new byte[500];

            var fal = ByteUtil.ToByteString(testBytes);

            ByteUtil.ToByteString(testBytes).Should().Equal(ByteString.CopyFrom(testBytes));
        }

        [Fact]
        public void Merge_ByteArrays_To_Single_Byte_Collection_Should_Succeed()
        {
            var firstBytesArray = GeneratePopulatedBytesArray(50);
            var secondBytesArray = GeneratePopulatedBytesArray(50);

            var byteResult = ByteUtil.Merge(firstBytesArray, secondBytesArray);

            byteResult.Take(50).Should().Contain(firstBytesArray);

            byteResult.TakeLast(50).Should().Contain(secondBytesArray);

            byteResult.Length.Should().Be(100);
        }

        [Fact]
        public void Append_Byte_To_ByteArrays_Should_Succeed()
        {
            var firstBytesArray = GeneratePopulatedBytesArray(50);
            var secondBytesArray = GeneratePopulatedBytesArray(1);

            var byteResult = ByteUtil.AppendByte(firstBytesArray, secondBytesArray.FirstOrDefault());

            byteResult.Take(50).Should().Contain(firstBytesArray);

            byteResult.TakeLast(1).Should().Contain(secondBytesArray.FirstOrDefault());

            byteResult.Length.Should().Be(51);
        }

        [Fact]
        public void Slice_Byte_To_ByteArrays_Should_Fail()
        {
            var firstBytesArray = GeneratePopulatedBytesArray(10);
            var secondBytesArray = GeneratePopulatedBytesArray(1);

            var byteResult = ByteUtil.Slice(firstBytesArray, 2, 5);

            byteResult.Take(50).Should().Contain(firstBytesArray);

            byteResult.TakeLast(1).Should().Contain(secondBytesArray.FirstOrDefault());

            byteResult.Length.Should().Be(51);
        }

        [Theory]
        [InlineData("15241576832799933607683.3208352565279684")]
        [InlineData("152415768327999.3208352565279684")]
        [InlineData("0.0152415765279684")]
        [InlineData("65.9905500565279684")]
        [InlineData("6103.2744992565279684")]
        [InlineData("459851.4226352565279684")]
        [InlineData("32241085.9904352565279684")]
        [InlineData("2086490962.5328352565279684")]
        [InlineData("119493365036.6008352565279684")]
        [InlineData("5502205858863.7208352565279684")]
        [InlineData("100")]
        [InlineData("0.00022")]
        public void ShouldParse(string value)
        {
            Assert.Equal(value, BigDecimal.Parse(value).ToString());
        }

        [Fact]
        public void ShouldCastToDecimal()
        {
            Assert.Equal(200.002m, (decimal) (BigDecimal) 200.002m);
            Assert.Equal(15241576832799933607683.320835m,
                (decimal) BigDecimal.Parse("15241576832799933607683.3208352565279684"));
            Assert.Equal(152415768327999.32083525652797m,
                (decimal) BigDecimal.Parse("152415768327999.3208352565279684"));
        }

        [Fact]
        public void ShouldCastToDouble()
        {
            Assert.Equal(200.002, (double) (BigDecimal) 200.002m);
            Assert.Equal(15241576832799933607683.320835,
                (double) BigDecimal.Parse("15241576832799933607683.3208352565279684"));
            Assert.Equal(152415768327999.320835, (double) BigDecimal.Parse("152415768327999.3208352565279684"));
        }

        [Fact]
        public void ShouldCastToInt()
        {
            Assert.Equal(200, (int) (BigDecimal) 200.002m);
        }

        private byte[] GeneratePopulatedBytesArray(int arraySize)
        {
            var testBytes = new byte[arraySize];
            new Random().NextBytes(testBytes);

            return testBytes;
        }
    }
}
