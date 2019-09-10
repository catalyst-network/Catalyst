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
using Catalyst.Protocol.Common;
using Catalyst.Protocol.DAO;
using Catalyst.Protocol.Extensions;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Protocol.UnitTests.DAO
{
    public class DaoCommonTests
    {
        [Fact]
        public static void ProtocolMessageDao_ProtocolMessage_Should_Be_Convertible()
        {
            var protocolMessageDao = new ProtocolMessageDao();

            var message = new ProtocolMessage
            {
                CorrelationId = Guid.NewGuid().ToByteString(),
                TypeUrl = "cleanurl",
                Value = "somecontent".ToUtf8ByteString(),
                PeerId = PeerIdentifierHelper.GetPeerIdentifier("testcontent").PeerId
            };

            var messageDao = protocolMessageDao.ToDao(message);
            var protoBuff = messageDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public static void ProtocolErrorMessageSignedDao_ProtocolErrorMessageSigned_Should_Be_Convertible()
        {
            var protocolErrorMessageSignedDao = new ProtocolErrorMessageSignedDao();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var message = new ProtocolErrorMessageSigned
            {
                CorrelationId = Guid.NewGuid().ToByteString(),
                Signature = byteRn.ToByteString(),
                PeerId = PeerIdentifierHelper.GetPeerIdentifier("test").PeerId,
                Code = 74
            };

            var errorMessageSignedDao = protocolErrorMessageSignedDao.ToDao(message);
            var protoBuff = errorMessageSignedDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public static void PeerIdDao_PeerId_Should_Be_Convertible()
        {
            var peerIdDao = new PeerIdDao();

            var message = PeerIdentifierHelper.GetPeerIdentifier("MyPeerId_Testing").PeerId;

            var peer = peerIdDao.ToDao(message);
            var protoBuff = peer.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public static void SigningContextDao_SigningContext_Should_Be_Convertible()
        {
            var signingContextDao = new SigningContextDao();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var message = new SigningContext
            {
                Network = Network.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };

            var contextDao = signingContextDao.ToDao(message);
            var protoBuff = contextDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }
    }
}
