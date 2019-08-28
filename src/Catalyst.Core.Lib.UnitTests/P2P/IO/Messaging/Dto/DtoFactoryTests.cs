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

using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P.IO.Messaging.Dto
{
    public sealed class DtoFactoryTests
    {
        [Fact]
        public void Can_Produce_a_Valid_Request_Dto()
        {
            var pingRequest = new PingRequest();
            var protocolMessage =
                pingRequest.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("im_a_sender").PeerId);
            var pingRequestDto = new MessageDto(protocolMessage,
                PeerIdentifierHelper.GetPeerIdentifier("im_a_recipient")
            );
            
            pingRequestDto.Should().BeAssignableTo<IMessageDto<ProtocolMessage>>();
            pingRequestDto.Content.FromProtocolMessage<PingRequest>().Should().Be(pingRequest);
            pingRequestDto.RecipientPeerIdentifier.Should().BeAssignableTo<IPeerIdentifier>();
            pingRequestDto.SenderPeerIdentifier.Should().BeAssignableTo<IPeerIdentifier>();
            pingRequestDto.Content.Should().BeAssignableTo<IMessage>();
        }
        
        [Fact]
        public void Can_Produce_a_Valid_Response_Dto()
        {
            var pingResponse = new PingResponse();
            var protocolMessage =
                pingResponse.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("im_a_sender").PeerId, CorrelationId.GenerateCorrelationId());
            var pingResponseDto = new MessageDto(protocolMessage,
                PeerIdentifierHelper.GetPeerIdentifier("im_a_recipient")
            );

            pingResponseDto.Should().BeAssignableTo<IMessageDto<ProtocolMessage>>();
            pingResponseDto.RecipientPeerIdentifier.Should().BeAssignableTo<IPeerIdentifier>();
            pingResponseDto.SenderPeerIdentifier.Should().BeAssignableTo<IPeerIdentifier>();
            pingResponseDto.Content.Should().BeAssignableTo<IMessage>();
            pingResponseDto.CorrelationId.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void Can_Produce_a_Valid_Broadcast_Dto()
        {
            var transactionDto = new MessageDto(
                new TransactionBroadcast().ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("im_a_sender").PeerId),
                PeerIdentifierHelper.GetPeerIdentifier("im_a_recipient")
            );
            
            transactionDto.Should().BeAssignableTo<IMessageDto<ProtocolMessage>>();
            transactionDto.RecipientPeerIdentifier.Should().BeAssignableTo<IPeerIdentifier>();
            transactionDto.SenderPeerIdentifier.Should().BeAssignableTo<IPeerIdentifier>();
            transactionDto.Content.Should().BeAssignableTo<IMessage>();
        }
    }
}
