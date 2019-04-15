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
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Protocol.IPPN;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public sealed class MessageDtoTests
    {
        private readonly MessageDto<IMessage> _messageDto;

        public MessageDtoTests()
        {
            var peerIdentifier = Substitute.For<IPeerIdentifier>();
            var pingRequest = Substitute.For<IMessage<PingRequest>>();
            _messageDto = new MessageDto<IMessage>(
                P2PMessageType.PingRequest,
                pingRequest,
                new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort), 
                peerIdentifier
            );
        }

        [Fact]
        public void CanInitMessageDtoCorrectly()
        {
            Assert.NotNull(_messageDto);
            _messageDto.Should().BeOfType(typeof(MessageDto<IMessage>));
            _messageDto.Type.Should().BeEquivalentTo(P2PMessageType.PingRequest);
            _messageDto.Message.Should().NotBeNull().And.BeAssignableTo(typeof(IMessage<PingRequest>));
            _messageDto.Destination.Should().NotBeNull().And.BeOfType(typeof(IPEndPoint));
            _messageDto.PeerIdentifier.Should().NotBeNull().And.BeAssignableTo(typeof(IPeerIdentifier));
        }
    }
}
