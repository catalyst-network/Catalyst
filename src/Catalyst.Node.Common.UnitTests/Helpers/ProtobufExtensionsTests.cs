/*
* Copyright(c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node<https: //github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node.If not, see<https: //www.gnu.org/licenses/>.
*/

using Catalyst.Node.Common.Helpers;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.Helpers
{
    public class ProtobufExtensionsTests
    {
        [Fact]
        public static void ShortenedFullName_should_remove_namespace_start()
        {
            Transaction.Descriptor.FullName.Should().Be("Catalyst.Protocol.Transaction.Transaction");
            Transaction.Descriptor.ShortenedFullName().Should().Be("Transaction.Transaction");
        }

        [Fact]
        public static void ShortenedProtoFullName_should_remove_namespace_start()
        {
            PeerProtocol.Types.PingRequest.Descriptor.FullName.Should().Be("Catalyst.Protocol.IPPN.PeerProtocol.PingRequest");
            typeof(PeerProtocol.Types.PingRequest).ShortenedProtoFullName().Should().Be("IPPN.PeerProtocol.PingRequest");
        }
    }
}
