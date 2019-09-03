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
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Util;
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

namespace Catalyst.Core.UnitTests.IO.Codecs
{
    public sealed class DatagramPacketDecoderTests
    {
        [Fact]
        public void DatagramPacketDecoder_Can_Decode_IMessage_With_ProtobufDecoder_And_ProtocolMessageSignedParser()
        {
            var channel = new EmbeddedChannel(
                new DatagramPacketDecoder(
                    new ProtobufDecoder(ProtocolMessageSigned.Parser)
                )
            );
            
            var protocolMessageSigned = new ProtocolMessageSigned
            {
                Message = new PingRequest().ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId, 
                    CorrelationId.GenerateCorrelationId()
                ),
                
                Signature = ByteUtil.GenerateRandomByteArray(64).ToByteString()
            };
            
            var datagramPacket = new DatagramPacket(
                Unpooled.WrappedBuffer(protocolMessageSigned.ToByteArray()),
                new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort),
                new IPEndPoint(IPAddress.Any, IPEndPoint.MaxPort)
            );

            Assert.True(channel.WriteInbound(datagramPacket));
            var content = channel.ReadInbound<ProtocolMessageSigned>();
            Assert.Equal(protocolMessageSigned, content);
            Assert.False(channel.Finish());
        }
    }
}
