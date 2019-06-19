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

using Catalyst.Common.Config;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Handlers
{
    public sealed class ProtoDatagramEncoderHandlerTests
    {
        private readonly IChannelHandlerContext _fakeContext;

        public ProtoDatagramEncoderHandlerTests()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
        }
        
        [Fact]
        public void Does_Process_IMessageDto_Types()
        {
            var fakeRequestMessageDto = Substitute.For<IMessageSignedDto<ProtocolMessageSigned>>();
            fakeRequestMessageDto.MessageType.Returns(MessageTypes.Request);
            fakeRequestMessageDto.Message.Returns(Substitute.For<IMessage<ProtocolMessageSigned>>());
            fakeRequestMessageDto.Sender.Returns(PeerIdentifierHelper.GetPeerIdentifier("Im_The_Sender"));

            var protoDatagramEncoderHandler = new ProtoDatagramEncoderHandler();
            protoDatagramEncoderHandler.WriteAsync(_fakeContext, fakeRequestMessageDto);

            _fakeContext.ReceivedWithAnyArgs(1).WriteAndFlushAsync(Arg.Any<IByteBufferHolder>());
        }
    }
}
