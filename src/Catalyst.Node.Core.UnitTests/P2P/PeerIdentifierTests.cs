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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Common;
using FluentAssertions;
using Google.Protobuf;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public class PeerIdentifierTests
    {
        private readonly ITestOutputHelper _output;
        private readonly PeerId _validPeerId;

        public PeerIdentifierTests(ITestOutputHelper output)
        {
            _output = output;
            _validPeerId = PeerIdHelper.GetPeerId();
        }

        [Fact]
        public void Valid_Peer_should_have_fields_with_correct_sizes()
        {
            _validPeerId.ClientId.ToByteArray().Length.Should().Be(2);
            _validPeerId.ClientVersion.ToByteArray().Length.Should().Be(2);
            _validPeerId.Ip.ToByteArray().Length.Should().Be(16);
            _validPeerId.Port.ToByteArray().Length.Should().Be(2);
            _validPeerId.PublicKey.ToByteArray().Length.Should().Be(20);

            _output.WriteLine(string.Join(" ", _validPeerId.ToByteArray()));
            var fieldsInBytes = new[]
            {
                _validPeerId.ClientId.ToByteArray(),
                _validPeerId.ClientVersion.ToByteArray(),
                _validPeerId.Ip.ToByteArray(), _validPeerId.Port.ToByteArray(),
                _validPeerId.PublicKey.ToByteArray()
            };
            _output.WriteLine(string.Join(" ", fieldsInBytes.SelectMany(b => b)));
        }

        // [Fact]
        // public void Constructor_should_accept_valid_byte_arrays()
        // {
        //     var newPeer = new PeerIdentifier(_validPeerId);
        //     newPeer.PublicKey.Length.Should().Be(20);
        //     newPeer.ClientId.Should().Be("Tc");
        //     newPeer.ClientVersion.Should().Be("01");
        //     newPeer.Ip.GetAddressBytes().Should()
        //        .Equal(IPAddress.Parse("127.0.0.1").To16Bytes());
        //     newPeer.Port.Should().Be(12345);
        //     _output.WriteLine(newPeer.ToString());
        // }

        [Theory]
        [SuppressMessage("ReSharper", "Duplicate")]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(19)]
        [InlineData(21)]
        public void Constructor_should_fail_on_wrong_public_key(int pubKeySize)
        {
            var invalidPeer = new PeerId(_validPeerId)
            {
                PublicKey = new byte[pubKeySize].ToByteString()
            };
            new Action(() => new PeerIdentifier(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("*PublicKey*");

            invalidPeer.PublicKey = new byte[21].ToByteString();
        }

        private class IpTestData : TheoryData<byte[]>
        {
            public IpTestData()
            {
                Add(new byte[0]);
                Add(new byte[4]);
                Add(new byte[15]);
                Add(new byte[17]);
            }
        }

        [Theory]
        [ClassData(typeof(IpTestData))]

        //Todo: discuss if this is relevant: why do we enforce a given size for IPs (or anything) if proto handles it
        public void Constructor_should_fail_on_wrong_ip(byte[] ipBytes)
        {
            var invalidPeer = new PeerId(_validPeerId)
            {
                Ip = ipBytes.ToByteString()
            };

            new Action(() => new PeerIdentifier(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("*Ip*");
        }

        [Theory]
        [InlineData("Mum")]
        [InlineData("Daddy")]
        [InlineData("20")]
        [InlineData("I2")]
        [InlineData("M+")]
        public void Constructor_should_fail_on_wrong_ClientId(string clientId)
        {
            var invalidPeer = new PeerId(_validPeerId)
            {
                ClientId = clientId.ToUtf8ByteString()
            };

            new Action(() => new PeerIdentifier(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("*ClientId*");
        }

        [Theory]
        [InlineData("1")]
        [InlineData("123")]
        [InlineData("1.6")]
        [InlineData("1.6.5")]
        [InlineData("0.0.1")]
        public void Constructor_should_fail_on_wrong_ClientVersion(string version)
        {
            var invalidPeer = new PeerId(_validPeerId)
            {
                ClientVersion = version.ToUtf8ByteString()
            };

            new Action(() => new PeerIdentifier(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("*clientVersion*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1024)]
        public void Constructor_should_fail_on_wrong_Port(ushort port)
        {
            var invalidPeer = new PeerId(_validPeerId)
            {
                Port = BitConverter.GetBytes(port).ToByteString()
            };

            new Action(() => new PeerIdentifier(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("*Port*");
        }
    }
}
