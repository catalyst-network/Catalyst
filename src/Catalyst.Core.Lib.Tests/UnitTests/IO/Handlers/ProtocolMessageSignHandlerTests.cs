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

using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Handlers;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.Tests.Fakes;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using Catalyst.TestUtils.Protocol;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.IO.Handlers
{
    public sealed class ProtocolMessageSignHandlerTests
    {
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IMessageDto<ProtocolMessage> _dto;
        private readonly FakeKeySigner _keySigner;

        public ProtocolMessageSignHandlerTests()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _keySigner = FakeKeySigner.SignOnly();

            _keySigner.Signature.SignatureBytes.Returns(ByteUtil.GenerateRandomByteArray(new FfiWrapper().SignatureLength));
            _keySigner.Signature.PublicKeyBytes.Returns(ByteUtil.GenerateRandomByteArray(new FfiWrapper().PublicKeyLength));

            _dto = new MessageDto(new PingRequest().ToProtocolMessage(PeerIdHelper.GetPeerId("sender")),
                PeerIdHelper.GetPeerId("recipient")
            );
        }

        [Fact]
        public void CantSignMessage()
        {
            var protocolMessageSignHandler = new ProtocolMessageSignHandler(_keySigner, DevNetPeerSigningContext.Instance);

            protocolMessageSignHandler?.WriteAsync(_fakeContext, new object());

            _keySigner.SignCount.Should().Be(0);
            _fakeContext.ReceivedWithAnyArgs()?.WriteAsync(new object());
        }

        [Fact]
        public void CanWriteAsyncOnSigningMessage()
        {
            var protocolMessageSignHandler = new ProtocolMessageSignHandler(_keySigner, DevNetPeerSigningContext.Instance);

            protocolMessageSignHandler?.WriteAsync(_fakeContext, _dto);
            
            _fakeContext.DidNotReceiveWithAnyArgs()?.WriteAndFlushAsync(new object());
            _fakeContext.ReceivedWithAnyArgs()?.WriteAsync(new object());
        }
    }
}
