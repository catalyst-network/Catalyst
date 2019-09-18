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

using System.Net;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels.Embedded;
using DotNetty.Transport.Channels.Sockets;
using Google.Protobuf;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.IO.Codecs
{
    public sealed class DatagramPacketEncoderTests
    {
        private readonly EmbeddedChannel _channel;
        private readonly PeerId _recipientPid;
        private readonly DatagramPacket _datagramPacket;
        private readonly ProtocolMessage _protocolMessageSigned;
        
        public DatagramPacketEncoderTests()
        {
            _channel = new EmbeddedChannel(
                new DatagramPacketEncoder<IMessage>(new ProtobufEncoder())
            );

            var senderPid = PeerIdHelper.GetPeerId("sender",
                IPAddress.Loopback,
                10000
            );
            
            _recipientPid = PeerIdHelper.GetPeerId("sender",
                IPAddress.Loopback,
                20000
            );

            _protocolMessageSigned = new PingRequest().ToSignedProtocolMessage(senderPid, (byte[]) default);
            
            _datagramPacket = new DatagramPacket(
                Unpooled.WrappedBuffer(_protocolMessageSigned.ToByteArray()),
                senderPid.IpEndPoint,
                _recipientPid.IpEndPoint
            );
        }

        [Fact]
        public void DatagramPacketEncoder_Can_Encode_IMessage_With_ProtobufEncoder()
        {
            Assert.True(_channel.WriteOutbound(new SignedMessageDto(_protocolMessageSigned, _recipientPid)));

            var datagramPacket = _channel.ReadOutbound<DatagramPacket>();
            Assert.NotNull(datagramPacket);

            Assert.Equal(_datagramPacket.Content, datagramPacket.Content);
            Assert.Equal(_datagramPacket.Sender, datagramPacket.Sender);
            Assert.Equal(_datagramPacket.Recipient, datagramPacket.Recipient);
            datagramPacket.Release();
            Assert.False(_channel.Finish());
        }

        [Fact]
        public void DatagramPacketEncoder_Will_Not_Encode_UnmatchedMessageType()
        {
            Assert.True(_channel.WriteOutbound(_protocolMessageSigned));
        
            var protocolMessageSigned = _channel.ReadOutbound<ProtocolMessage>();
            Assert.NotNull(protocolMessageSigned);
            Assert.Same(_protocolMessageSigned, protocolMessageSigned);
            Assert.False(_channel.Finish());
        }
        
        [Fact]
        public void DatagramPacketEncoder_Will_Not_Encode_UnmatchedType()
        {
            const string expected = "junk";
            Assert.True(_channel.WriteOutbound(expected));
        
            var content = _channel.ReadOutbound<string>();
            Assert.Same(expected, content);
            Assert.False(_channel.Finish());
        }
    }
}
