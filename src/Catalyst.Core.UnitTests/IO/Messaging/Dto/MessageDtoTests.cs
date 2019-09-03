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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Messaging.Dto
{
    public sealed class MessageDtoTests
    {
        private readonly IMessageDto<ProtocolMessage> _messageDto;

        public MessageDtoTests()
        {
            var pingRequest = new PingRequest();
            _messageDto = new MessageDto(pingRequest.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("Sender_Key").PeerId),
                PeerIdentifierHelper.GetPeerIdentifier("Recipient_Key")
            );
        }

        [Fact]
        public void CanInitMessageDtoCorrectly()
        {
            Assert.NotNull(_messageDto);

            _messageDto.Should().BeOfType<MessageDto>();
            _messageDto.Content.Should().NotBeNull().And.BeAssignableTo(typeof(IMessage<ProtocolMessage>));
            _messageDto.RecipientPeerIdentifier.Should().NotBeNull().And.BeAssignableTo(typeof(IPeerIdentifier));
            _messageDto.SenderPeerIdentifier.Should().NotBeNull().And.BeAssignableTo(typeof(IPeerIdentifier));
        }
    }
}
