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

using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Messaging.Dto
{
    public sealed class MessageDtoTest
    {
        private readonly MessageDto _messageDto;

        public MessageDtoTest()
        {
            var peerIdentifier = Substitute.For<IPeerIdentifier>();
            var pingRequest = Substitute.For<IMessage<PingRequest>>();
            _messageDto = new MessageDto(
                pingRequest,
                MessageTypes.Request,
                PeerIdentifierHelper.GetPeerIdentifier("im_a_recipient"), 
                peerIdentifier
            );
        }

        [Fact]
        public void CanInitMessageDtoCorrectly()
        {
            Assert.NotNull(_messageDto);
            AssertionExtensions.Should(_messageDto).BeOfType(typeof(MessageDto));
            AssertionExtensions.Should(_messageDto.Message).NotBeNull().And.BeAssignableTo(typeof(IMessage<PingRequest>));
            AssertionExtensions.Should(_messageDto.Recipient).NotBeNull().And.BeAssignableTo(typeof(IPeerIdentifier));
            AssertionExtensions.Should(_messageDto.Sender).NotBeNull().And.BeAssignableTo(typeof(IPeerIdentifier));
        }
    }
}
