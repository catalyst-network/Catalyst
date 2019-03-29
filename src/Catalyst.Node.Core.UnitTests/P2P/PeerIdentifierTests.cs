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
using System.Net;
using System.Text;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Core.P2P;
using Catalyst.Protocol.Common;
using FluentAssertions;
using Google.Protobuf;
using Nethereum.RLP;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public class PeerIdentifierTests
    {
        private readonly PeerId _validPeer;

        public PeerIdentifierTests()
        {
            _validPeer = new PeerId()
            {
                PublicKey = new byte[20].ToByteString(),
                ClientId = Encoding.UTF8.GetBytes("aM").ToByteString(),
                ClientVersion = PeerIdentifier.AssemblyMajorVersion2Bytes.ToByteString(),
                Ip = IPAddress.Parse("127.0.0.1").To16Bytes().ToByteString(),
                Port = 12345.ToBytesForRLPEncoding().ToByteString()
            };
        }

        [Fact]
        public void Constructor_should_only_accept_valid_byte_arrays()
        {
            var newPeer = new PeerIdentifier(_validPeer);
            newPeer.Id.Length.Should().Be(42);
            newPeer.PeerId.PublicKey.Length.Should().Be(20);
            newPeer.PeerId.ClientId.Should().Equal(Encoding.UTF8.GetBytes("aM").ToByteString());
            //newPeer.PeerId.Port.Should().Equal(Encoding.UTF8.GetBytes("aM").ToByteString());
        }
    }
}
