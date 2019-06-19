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
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging.Dto;
using Catalyst.Common.IO.Messaging;
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
            
            pingRequestDto.Should().BeOfType<IMessageDto>();
            pingRequestDto.Recipient.Should().BeOfType<IPeerIdentifier>();
            pingRequestDto.Sender.Should().BeOfType<IPeerIdentifier>();
            pingRequestDto.Message.Should().BeOfType<IMessage>();
        }
        
        [Fact]
        public void Can_Produce_a_Valid_Response_Dto()
        {
            var pingResponseDto = new DtoFactory().GetDto(new PingResponse(),
                PeerIdentifierHelper.GetPeerIdentifier("im_a_recipient"),
                PeerIdentifierHelper.GetPeerIdentifier("im_a_sender"),
                Guid.NewGuid()
            );

            pingResponseDto.Should().BeOfType<IMessageDto>();
            pingResponseDto.Recipient.Should().BeOfType<IPeerIdentifier>();
            pingResponseDto.Sender.Should().BeOfType<IPeerIdentifier>();
            pingResponseDto.Message.Should().BeOfType<IMessage>();
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
            
            transactionDto.Should().BeOfType<IMessageDto>();
            transactionDto.Recipient.Should().BeOfType<IPeerIdentifier>();
            transactionDto.Sender.Should().BeOfType<IPeerIdentifier>();
            transactionDto.Message.Should().BeOfType<IMessage>();
        }
    }
}
