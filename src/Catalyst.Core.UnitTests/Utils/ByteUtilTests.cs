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
using Catalyst.Core.Util;
using FluentAssertions;
using Google.Protobuf;
using Xunit;

namespace Catalyst.Core.UnitTests.Utils
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

            testBytes.ToByteString().Should().Equal(ByteString.CopyFrom(testBytes));
        }

        [Fact]
        public void Convert_ByteArray_To_ByteString_Should_Fail()
        {
            var testBytes = new byte[500];

            testBytes.ToByteString().Should().Equal(ByteString.CopyFrom(testBytes));
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

        [Theory]
        [InlineData(2, 5)]
        [InlineData(78, 90)]
        [InlineData(10, 35)]
        public void Slice_Byte_To_ByteArrays_Should_Succeed(int st, int ed)
        {
            var firstBytesArray = GeneratePopulatedBytesArray(100);

            var byteResult = firstBytesArray.Slice(st, ed);

            for (int i = st, k = 0; i <= ed && k < (ed - st); i++, k++)
            {
                firstBytesArray[i].Should().Be(byteResult[k]);
            }
        }

        [Theory]
        [InlineData(78)]
        [InlineData(10)]
        public void Initialise_Empty_ByteArray_Should_Succeed(int arraySize)
        {
            var byteResult = ByteUtil.InitialiseEmptyByteArray(arraySize);

            var testBytes = new byte[arraySize];

            byteResult.Should().Equal(testBytes);
        }

        private byte[] GeneratePopulatedBytesArray(int arraySize)
        {
            var testBytes = new byte[arraySize];
            new Random().NextBytes(testBytes);

            return testBytes;
        }
    }
}
