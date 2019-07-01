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
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels.Embedded;
using DotNetty.Transport.Channels.Sockets;
using Google.Protobuf;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Codecs
{
    public sealed class DatagramPacketEncoderTests
    {
        private readonly EmbeddedChannel _channel;
        private readonly IPeerIdentifier _recipientPid;
        private readonly IPeerIdentifier _senderPid;
        private readonly DatagramPacket _datagramPacket;
        private readonly ProtocolMessageSigned _protocolMessageSigned;
        
        public DatagramPacketEncoderTests()
        {
            _channel = new EmbeddedChannel(
                new DatagramPacketEncoder<IMessage>(new ProtobufEncoder())
            );

            _senderPid = PeerIdentifierHelper.GetPeerIdentifier("sender", 
                "TC", 
                1,
                IPAddress.Loopback,
                10000
            );
            
            _recipientPid = PeerIdentifierHelper.GetPeerIdentifier("sender", 
                "TC",
                1,
                IPAddress.Loopback,
                20000
            );

            _protocolMessageSigned = new ProtocolMessageSigned
            {
                Message = new PingRequest().ToProtocolMessage(_senderPid.PeerId, Guid.NewGuid()),
                Signature = ByteUtil.GenerateRandomByteArray(64).ToByteString()
            };
            
            _datagramPacket = new DatagramPacket(
                Unpooled.WrappedBuffer(_protocolMessageSigned.ToByteArray()),
                _senderPid.IpEndPoint,
                _recipientPid.IpEndPoint
            );
        }
        
        [Fact]
        public void DatagramPacketEncoder_Can_Encode_IMessage_With_ProtobufEncoder()
        {
            Assert.True(_channel.WriteOutbound(new MessageDto<ProtocolMessageSigned>(
                _protocolMessageSigned,
                _senderPid,
                _recipientPid,
                Guid.NewGuid())));

            var datagramPacket = _channel.ReadOutbound<DatagramPacket>();
            Assert.NotNull(datagramPacket);
            
            try
            {
                Assert.Equal(_datagramPacket.Content, datagramPacket.Content);
                Assert.Equal(_datagramPacket.Sender, datagramPacket.Sender);
                Assert.Equal(_datagramPacket.Recipient, datagramPacket.Recipient);
            }
            finally
            {
                datagramPacket.Release();
                Assert.False(_channel.Finish());
            }
        }

        [Fact]
        public void DatagramPacketEncoder_Will_Not_Encode_UnmatchedMessageType()
        {
            Assert.True(_channel.WriteOutbound(_protocolMessageSigned));
        
            var protocolMessageSigned = _channel.ReadOutbound<ProtocolMessageSigned>();
            Assert.NotNull(protocolMessageSigned);
            try
            {
                Assert.Same(_protocolMessageSigned, protocolMessageSigned);
            }
            finally
            {
                Assert.False(_channel.Finish());
            }
        }
        
        [Fact]
        public void DatagramPacketEncoder_Will_Not_Encode_UnmatchedType()
        {
            try
            {
                const string expected = "junk";
                Assert.True(_channel.WriteOutbound(expected));
        
                var content = _channel.ReadOutbound<string>();
                Assert.Same(expected, content);
            }
            finally
            {
                Assert.False(_channel.Finish());
            }
        }
    }
}
