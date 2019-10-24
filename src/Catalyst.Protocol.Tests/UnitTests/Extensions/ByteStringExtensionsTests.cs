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

using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using FluentAssertions;
using Google.Protobuf;
using Xunit;

namespace Catalyst.Protocol.Tests.UnitTests.Extensions
{
    public class ByteStringExtensionsTests
    {
        [Theory]
        [InlineData(2)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(10000)]
        [InlineData(5000000)]
        public void Convert_ByteArray_To_ByteString_Should_Succeed(int arraySize)
        {
            var testBytes = ByteUtil.GenerateRandomByteArray(arraySize);

            testBytes.ToByteString().Should().Equal(ByteString.CopyFrom(testBytes));
        }

        [Fact]
        public void Convert_ByteArray_To_ByteString_Should_Fail()
        {
            var testBytes = new byte[500];

            testBytes.ToByteString().Should().Equal(ByteString.CopyFrom(testBytes));
        }
    }
}

