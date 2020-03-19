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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P
{
    public sealed class PeerIdValidatorTests
    {
        private readonly TestContext _output;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly PeerId _validPeerId;

        public PeerIdValidatorTests(TestContext output)
        {
            _output = output;
            _validPeerId = PeerIdHelper.GetPeerId();
            _peerIdValidator = new PeerIdValidator(new FfiWrapper());
        }

        [Test]
        public void Can_Validate_PeerId_Format()
        {
            _peerIdValidator.ValidatePeerIdFormat(_validPeerId);

            TestContext.WriteLine(string.Join(" ", _validPeerId.ToByteArray()));
            var fieldsInBytes = new[]
            {
                _validPeerId.Ip.ToByteArray(), BitConverter.GetBytes(_validPeerId.Port),
                _validPeerId.PublicKey.ToByteArray()
            };
            TestContext.WriteLine(string.Join(" ", fieldsInBytes.SelectMany(b => b)));
        }

        [Theory]
        [TestCase(0)]
        [TestCase(10)]
        [TestCase(19)]
        [TestCase(21)]
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

        private sealed class IpTestData : List<byte[]>
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
        [TestCase(typeof(IpTestData))]

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
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(1024)]
        public void Can_Throw_Argument_Exception_On_Wrong_Port(ushort port)
        {
            var invalidPeer = new PeerId(_validPeerId)
            {
                Port = port
            };

            new Action(() => _peerIdValidator.ValidatePeerIdFormat(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("*Port*");
        }
    }
}
