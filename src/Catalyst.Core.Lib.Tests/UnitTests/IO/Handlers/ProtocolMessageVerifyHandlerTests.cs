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

using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Lib.IO.Handlers;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using Catalyst.TestUtils.Protocol;
using DotNetty.Transport.Channels;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.IO.Handlers
{
    public sealed class ProtocolMessageVerifyHandlerTests
    {
        private readonly IChannelHandlerContext _fakeContext;
        private readonly ProtocolMessage _protocolMessageSigned;
        private readonly FakeKeySigner _keySigner;
        private readonly SigningContext _signingContext;

        public ProtocolMessageVerifyHandlerTests()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _keySigner = Substitute.For<FakeKeySigner>();
            _signingContext = DevNetPeerSigningContext.Instance;

            var signatureBytes = ByteUtil.GenerateRandomByteArray(new FfiWrapper().SignatureLength);
            var publicKeyBytes = ByteUtil.GenerateRandomByteArray(new FfiWrapper().PublicKeyLength);
            var peerId = PeerIdHelper.GetPeerId(publicKeyBytes);

            _protocolMessageSigned = new PingRequest()
               .ToSignedProtocolMessage(peerId, signatureBytes, _signingContext)
               .ToSignedProtocolMessage(peerId, signatureBytes, _signingContext);
        }

        [Fact]
        private void CanFireNextPipelineOnValidSignature()
        {
            _keySigner.Verify(Arg.Any<ISignature>(), Arg.Any<byte[]>(), default)
               .ReturnsForAnyArgs(true);

            var signatureHandler = new ProtocolMessageVerifyHandler(_keySigner);

            signatureHandler.ChannelRead(_fakeContext, _protocolMessageSigned);

            _fakeContext.ReceivedWithAnyArgs().FireChannelRead(_protocolMessageSigned).Received(1);
        }
        
        [Fact]
        private void CanFireNextPipelineOnInvalidSignature()
        {
            _keySigner.Verify(Arg.Any<ISignature>(), Arg.Any<byte[]>(), default)
               .ReturnsForAnyArgs(false);

            var signatureHandler = new ProtocolMessageVerifyHandler(_keySigner);

            signatureHandler.ChannelRead(_fakeContext, _protocolMessageSigned);
            
            _fakeContext.DidNotReceiveWithAnyArgs().FireChannelRead(_protocolMessageSigned).Received(0);
        }
    }
}
