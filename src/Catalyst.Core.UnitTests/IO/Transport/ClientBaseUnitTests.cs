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

using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Transport
{
    public sealed class ClientBaseUnitTests
    {
        [Fact]
        public void SendMessage_Should_Write_Message_To_Channel()
        {
            var messageDto = Substitute.For<IMessageDto<ProtocolMessage>>();
            var channelFactory = Substitute.For<ITcpClientChannelFactory>();
            var logger = Substitute.For<ILogger>();
            var eventLoopGroupFactory = Substitute.For<IEventLoopGroupFactory>();

            var testClientBase = new TestClientBase(channelFactory, logger, eventLoopGroupFactory);
            testClientBase.SendMessage(messageDto);

            testClientBase.Channel.Received(1).WriteAsync(messageDto);
        }
    }
}
