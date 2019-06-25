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
using Catalyst.Common.Cryptography;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.P2P
{
    public sealed class PeerIdValidatorTests
    {
        private readonly ITestOutputHelper _output;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly PeerId _validPeerId;

        public PeerIdValidatorTests(ITestOutputHelper output)
        {
            _output = output;
            _validPeerId = PeerIdHelper.GetPeerId();
            _peerIdValidator = new PeerIdValidator(new CryptoContext(new CryptoWrapper()), new PeerIdClientVersion("AC"));
        }

        [Fact]
        public void Valid_Peer_should_have_fields_with_correct_sizes()
        {
            _peerIdValidator.ValidatePeerIdFormat(_validPeerId);

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

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(19)]
        [InlineData(21)]
        public void Can_Throw_Argument_Exception_On_Invalid_Public_Key(int pubKeySize)
        {
            var invalidPeer = new PeerId(_validPeerId)
            {
                PublicKey = new byte[pubKeySize].ToByteString()
            };
            new Action(() => _peerIdValidator.ValidatePeerIdFormat(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("*PublicKey*");

            invalidPeer.PublicKey = new byte[21].ToByteString();
        }

        private sealed class IpTestData : TheoryData<byte[]>
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
        public void Can_Throw_Argument_Exception_On_Invalid_Ip(byte[] ipBytes)
        {
            var invalidPeer = new PeerId(_validPeerId)
            {
                Ip = ipBytes.ToByteString()
            };

            new Action(() => _peerIdValidator.ValidatePeerIdFormat(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("*Ip*");
        }

        [Theory]
        [InlineData("20")]
        [InlineData("I2")]
        [InlineData("M+")]
        public void Can_Throw_Argument_Exception_On_Invalid_Client_Id(string clientId)
        {
            var invalidPeer = new PeerId(_validPeerId)
            {
                ClientId = clientId.ToUtf8ByteString()
            };

            new Action(() => _peerIdValidator.ValidatePeerIdFormat(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("*ClientId*");
        }

        [Theory]
        [InlineData("1")]
        [InlineData("123")]
        [InlineData("1.6")]
        [InlineData("1.6.5")]
        [InlineData("0.0.1")]
        public void Can_Throw_Argument_Exception_On_Wrong_Client_Version(string version)
        {
            var invalidPeer = new PeerId(_validPeerId)
            {
                ClientVersion = version.ToUtf8ByteString()
            };

            new Action(() => _peerIdValidator.ValidatePeerIdFormat(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("*clientVersion*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1024)]
        public void Can_Throw_Argument_Exception_On_Wrong_Port(ushort port)
        {
            var invalidPeer = new PeerId(_validPeerId)
            {
                Port = BitConverter.GetBytes(port).ToByteString()
            };

            new Action(() => _peerIdValidator.ValidatePeerIdFormat(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("*Port*");
        }
    }
}
