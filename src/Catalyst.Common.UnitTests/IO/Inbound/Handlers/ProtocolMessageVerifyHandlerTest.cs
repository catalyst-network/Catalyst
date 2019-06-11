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

using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Inbound.Handlers;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using NSubstitute;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Inbound.Handlers
{
    public sealed class ProtocolMessageVerifyHandlerTest
    {
        private readonly IChannelHandlerContext _fakeContext;
        private readonly ProtocolMessageSigned _protocolMessageSigned;
        private readonly IKeySigner _keySigner;

        public ProtocolMessageVerifyHandlerTest()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _keySigner = Substitute.For<IKeySigner>();

            _protocolMessageSigned = new ProtocolMessageSigned
            {
                Signature = new Signature(ByteUtil.GenerateRandomByteArray(64)).Bytes.RawBytes.ToByteString(),
                Message = new ProtocolMessage
                {
                    PeerId = PeerIdentifierHelper.GetPeerIdentifier(ByteUtil.GenerateRandomByteArray(32).ToString()).PeerId
                }
            };
        }

        [Fact]
        private void CanFireNextPipelineOnValidSignature()
        {
            _keySigner.Verify(Arg.Any<PublicKey>(), Arg.Any<byte[]>(), Arg.Any<Signature>())
               .Returns(true);

            var signatureHandler = new ProtocolMessageVerifyHandler(_keySigner);

            signatureHandler.ChannelRead(_fakeContext, _protocolMessageSigned);

            _fakeContext.ReceivedWithAnyArgs().FireChannelRead(_protocolMessageSigned).Received(1);
            _fakeContext.DidNotReceiveWithAnyArgs().CloseAsync();
        }
        
        [Fact]
        private void CanFireNextPipelineOnInvalidSignature()
        {
            _keySigner.Verify(Arg.Any<PublicKey>(), Arg.Any<byte[]>(), Arg.Any<Signature>())
               .Returns(false);

            var signatureHandler = new ProtocolMessageVerifyHandler(_keySigner);

            signatureHandler.ChannelRead(_fakeContext, _protocolMessageSigned);

            _fakeContext.DidNotReceiveWithAnyArgs().FireChannelRead(_protocolMessageSigned).Received(0);
            _fakeContext.ReceivedWithAnyArgs().CloseAsync();
        }
    }
}
