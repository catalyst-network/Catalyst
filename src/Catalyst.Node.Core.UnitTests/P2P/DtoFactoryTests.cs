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
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P
{
    public sealed class DtoFactoryTests
    {
        [Fact]
        public void Can_Produce_a_Valid_Request_Dto()
        {
            var pingRequestDto = new DtoFactory().GetDto(new PingRequest(),
                PeerIdentifierHelper.GetPeerIdentifier("im_a_recipient"),
                PeerIdentifierHelper.GetPeerIdentifier("im_a_sender")
            );
            
            pingRequestDto.Should().BeAssignableTo<IMessageDto>();
            pingRequestDto.Recipient.Should().BeAssignableTo<IPeerIdentifier>();
            pingRequestDto.Sender.Should().BeAssignableTo<IPeerIdentifier>();
            pingRequestDto.Message.Should().BeAssignableTo<IMessage>();
        }
        
        [Fact]
        public void Can_Produce_a_Valid_Response_Dto()
        {
            var pingResponseDto = new DtoFactory().GetDto(new PingResponse(),
                PeerIdentifierHelper.GetPeerIdentifier("im_a_recipient"),
                PeerIdentifierHelper.GetPeerIdentifier("im_a_sender"),
                Guid.NewGuid()
            );

            pingResponseDto.Should().BeAssignableTo<IMessageDto>();
            pingResponseDto.Recipient.Should().BeAssignableTo<IPeerIdentifier>();
            pingResponseDto.Sender.Should().BeAssignableTo<IPeerIdentifier>();
            pingResponseDto.Message.Should().BeAssignableTo<IMessage>();
            pingResponseDto.CorrelationId.Should().NotBeEmpty();
        }

        [Fact]
        public void Can_Produce_a_Valid_Broadcast_Dto()
        {
            var transactionDto = new DtoFactory().GetDto(
                new TransactionBroadcast(),
                PeerIdentifierHelper.GetPeerIdentifier("im_a_recipient"),
                PeerIdentifierHelper.GetPeerIdentifier("im_a_sender")
            );
            
            transactionDto.Should().BeAssignableTo<IMessageDto>();
            transactionDto.Recipient.Should().BeAssignableTo<IPeerIdentifier>();
            transactionDto.Sender.Should().BeAssignableTo<IPeerIdentifier>();
            transactionDto.Message.Should().BeAssignableTo<IMessage>();
        }
    }
}
