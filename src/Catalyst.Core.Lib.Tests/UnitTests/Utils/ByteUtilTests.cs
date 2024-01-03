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
using Catalyst.Core.Lib.Util;
using FluentAssertions;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.Utils
{
    public sealed class ByteUtilTests
    {
        [Test]
        public void Merge_ByteArrays_To_Single_Byte_Collection_Should_Succeed()
        {
            var firstBytesArray = GeneratePopulatedBytesArray(50);
            var secondBytesArray = GeneratePopulatedBytesArray(50);

            var byteResult = ByteUtil.Merge(firstBytesArray, secondBytesArray);

            byteResult.Take(50).Should().Contain(firstBytesArray);

            byteResult.TakeLast(50).Should().Contain(secondBytesArray);

            byteResult.Length.Should().Be(100);
        }

        [Test]
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
        [TestCase(78)]
        [TestCase(10)]
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
