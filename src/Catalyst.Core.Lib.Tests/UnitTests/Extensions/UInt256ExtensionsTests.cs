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
using FluentAssertions;
using Nethermind.Dirichlet.Numerics;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.Extensions
{
    public class UInt256ExtensionsTests
    {
        private sealed class UInt256ConversionTestData : TheoryData<UInt256>
        {
            public UInt256ConversionTestData()
            {
                Add(UInt256.MinValue);
                Add(UInt256.Zero);
                Add(UInt256.One);
                Add(UInt256.MaxValue);
                Add(new UInt256(120328857));
                Add(new UInt256(234));
                Add(UInt256.Parse("2451109321324845879858375942387509287520984375938756894375494387"));
            }
        }

        [Theory]
        [ClassData(typeof(UInt256ConversionTestData))]
        public void UInt256_ToByteString_And_Back_Should_Keep_Value_Intact(UInt256 bigInt)
        {
            var x = (UInt256) 234;
            var bytesString = x.ToUint256ByteString();
            var xAgain = bytesString.ToUInt256();

            xAgain.S0.Should().Be(x.S0);
            xAgain.S1.Should().Be(x.S1);
            xAgain.S2.Should().Be(x.S2);
            xAgain.S3.Should().Be(x.S3);
        }
    }
}

