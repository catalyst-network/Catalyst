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

using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Codecs;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Codecs
{
    public class AddressedEnvelopeToIMessageEncoderTests
    {
        private readonly EmbeddedChannel _testChannel;

        public AddressedEnvelopeToIMessageEncoderTests()
        {
            _testChannel = new EmbeddedChannel(new AddressedEnvelopeToIMessageEncoder());
        }

        [Fact]
        public void Can_Encode_Signed_Message_Dto_To_Protocol_Message_Signed()
        {
            var messageDto = new SignedMessageDto(new ProtocolMessageSigned
                {
                    Message = new ProtocolMessage
                    {
                        PeerId = PeerIdHelper.GetPeerId("TestSender"),
                        CorrelationId = CorrelationId.GenerateCorrelationId().Id.ToByteString()
                    }
                },
                PeerIdentifierHelper.GetPeerIdentifier("Test"));
            _testChannel.WriteOutbound(messageDto);
            var outboundMessages = _testChannel.OutboundMessages.ToArray();
            outboundMessages.Length.Should().Be(1);
            outboundMessages[0].Should().BeAssignableTo<ProtocolMessageSigned>();
        }
    }
}
