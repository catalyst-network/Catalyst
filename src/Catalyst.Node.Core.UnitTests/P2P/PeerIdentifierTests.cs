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
using System.Net;
using System.Text;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Core.P2P;
using Catalyst.Protocol.Common;
using FluentAssertions;
using Google.Protobuf;
using Nethereum.RLP;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public class PeerIdentifierTests
    {
        private readonly ITestOutputHelper _output;
        private readonly PeerId _validPeer;

        public PeerIdentifierTests(ITestOutputHelper output)
        {
            _output = output;
            _validPeer = new PeerId()
            {
                PublicKey = new byte[20].ToByteString(),
                ClientId = "aM".ToUtf8ByteString(),
                ClientVersion = "09".ToUtf8ByteString(),
                Ip = IPAddress.Parse("127.0.0.1").To16Bytes().ToByteString(),
                Port = BitConverter.GetBytes(12345).ToByteString()
            };
        }

        [Fact]
        public void Valid_Peer_should_have_fields_with_correct_sizes()
        {
            _validPeer.ClientId.ToByteArray().Length.Should().Be(2);
            _validPeer.ClientVersion.ToByteArray().Length.Should().Be(2);
            _validPeer.Ip.ToByteArray().Length.Should().Be(16);
            _validPeer.Port.ToByteArray().Length.Should().Be(2);
            _validPeer.PublicKey.ToByteArray().Length.Should().Be(20);

            _output.WriteLine(string.Join(" ", _validPeer.ToByteArray()));
            var fieldsInBytes = new[]
            {
                _validPeer.ClientId.ToByteArray(),
                _validPeer.ClientVersion.ToByteArray(),
                _validPeer.Ip.ToByteArray(), _validPeer.Port.ToByteArray(),
                _validPeer.PublicKey.ToByteArray()
            };
            _output.WriteLine(string.Join(" ", fieldsInBytes.SelectMany(b => b)));
        }

        [Fact]
        public void Constructor_should_accept_valid_byte_arrays()
        {
            var newPeer = new PeerIdentifier(_validPeer);
            newPeer.PublicKey.Length.Should().Be(20);
            newPeer.ClientId.Should().Be("aM");
            newPeer.ClientVersion.Should().Be("09");
            newPeer.Ip.GetAddressBytes().Should()
               .Equal(IPAddress.Parse("127.0.0.1").To16Bytes());
            newPeer.Port.Should().Be(12345);

            _output.WriteLine(newPeer.ToString());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(19)]
        [InlineData(21)]
        public void Constructor_should_fail_on_wrong_public_key(int pubKeySize)
        {
            var invalidPeer = new PeerId(_validPeer)
            {
                PublicKey = new byte[pubKeySize].ToByteString()
            };
            new Action(() => new PeerIdentifier(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage( "*PublicKey*");

            invalidPeer.PublicKey = new byte[21].ToByteString();
        }
    }
}
