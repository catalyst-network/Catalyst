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
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.IO.Messaging.Dto
{
    public sealed class MessageDtoTests
    {
        private readonly IMessageDto<ProtocolMessage> _messageDto;

        public MessageDtoTests()
        {
            var pingRequest = new PingRequest();
            _messageDto = new MessageDto(pingRequest.ToProtocolMessage(PeerIdHelper.GetPeerId("Sender_Key")),
                PeerIdHelper.GetPeerId("Recipient_Key")
            );
        }

        [Test]
        public void CanInitMessageDtoCorrectly()
        {
            Assert.That(_messageDto, Is.Null);

            _messageDto.Should().BeOfType<MessageDto>();
            _messageDto.Content.Should().NotBeNull().And.BeAssignableTo(typeof(IMessage<ProtocolMessage>));
            _messageDto.RecipientPeerIdentifier.Should().NotBeNull();
            _messageDto.SenderPeerIdentifier.Should().NotBeNull();
        }
    }
}
