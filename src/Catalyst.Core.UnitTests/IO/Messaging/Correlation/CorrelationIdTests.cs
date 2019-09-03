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

using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Util;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Messaging.Correlation
{
    public class CorrelationIdTests
    {
        [Fact]
        public void CorrelationId_Be_Equal_When_Ids_Are_Equal()
        {
            var baseBytes = ByteUtil.GenerateRandomByteArray(CorrelationId.GuidByteLength);

            var id1 = new CorrelationId(baseBytes);
            var id2 = new CorrelationId(baseBytes);

            id1.GetHashCode().Should().Be(id2.GetHashCode());
            id1.Equals(id2).Should().BeTrue();
            (id1 == id2).Should().BeTrue();
        }

        [Fact]
        public void CorrelationId_Not_Be_Equal_When_Ids_Are_Not_Equal()
        {
            var baseBytes = ByteUtil.GenerateRandomByteArray(CorrelationId.GuidByteLength);
            var differentBytes = ByteUtil.GenerateRandomByteArray(CorrelationId.GuidByteLength);

            var id1 = new CorrelationId(baseBytes);
            var id2 = (CorrelationId) null;
            var id3 = new CorrelationId(differentBytes);
            
            id1.Equals(id2).Should().BeFalse();
            id1.Equals(id3).Should().BeFalse();
            (id1 == id2).Should().BeFalse();
            (id1 == id3).Should().BeFalse();
        }

        [Fact]
        public void ToString_Should_Be_The_Same_As_Guid_ToString()
        {
            var baseBytes = ByteUtil.GenerateRandomByteArray(CorrelationId.GuidByteLength);

            var id1 = new CorrelationId(baseBytes);
            id1.ToString().Should().Be(id1.Id.ToString());
        }
    }
}

