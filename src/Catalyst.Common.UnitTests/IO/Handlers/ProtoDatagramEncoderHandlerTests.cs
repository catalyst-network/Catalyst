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
using System.Net;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using DotNetty.Transport.Channels.Sockets;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Handlers
{
    public sealed class ProtoDatagramEncoderHandlerTests
    {
        [Fact]
        public void Does_Process_IMessageDto_Types()
        {
            // var handler = new DatagramProtobufEncoder(Substitute.For<ILogger>());
            var handler = new DatagramPacketEncoder<IMessage>(
                new ProtobufEncoder()
            );

            var channel = new EmbeddedChannel(handler);

            var protocolMessageSigned = new ProtocolMessageSigned
            {
                Message = new PingRequest().ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId, Guid.NewGuid()),
                Signature = ByteUtil.GenerateRandomByteArray(64).ToByteString()
            };
           
            var datagram = new DatagramPacket(Unpooled.WrappedBuffer(protocolMessageSigned.Message.ToByteArray()), new IPEndPoint(IPAddress.Loopback, IPEndPoint.MinPort));
                
            channel.WriteOutbound(datagram);
            var packet = channel.ReadOutbound<DatagramPacket>();
            packet.Content.Should().BeAssignableTo<IByteBuffer>();
            
            var decoder = new DatagramPacketDecoder(new ProtobufDecoder(ProtocolMessageSigned.Parser));
            var decoderChannel = new EmbeddedChannel(decoder);
            decoderChannel.WriteInbound(new ProtocolMessageSigned());
            var message = decoderChannel.ReadInbound<IMessage>();
            message.Should().BeAssignableTo<IMessage>();
        }
    }
}
