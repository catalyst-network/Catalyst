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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P.Messaging.Dto;
using Catalyst.Common.Util;
using Catalyst.Node.Core.RPC.Observables;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC.Observables
{
    public sealed class GetVersionRequestObserverTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;

        public GetVersionRequestObserverTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();

            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
            _fakeContext.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));
        }

        [Fact]
        public async Task Valid_GetVersion_Request_Should_Send_VersionResponse()
        {
            var versionRequest = new VersionRequest().ToProtocolMessage(PeerIdHelper.GetPeerId(), Guid.NewGuid());
            
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, versionRequest);
            var handler = new GetVersionRequestObserver(PeerIdentifierHelper.GetPeerIdentifier("sender"), _logger);
            
            handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolScheduler();
            
            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto) receivedCalls.Single().GetArguments().Single();
            sentResponseDto.Message.Descriptor.ShortenedFullName().Should().Be(VersionResponse.Descriptor.ShortenedFullName());
            var versionResponseMessage = (VersionResponse) sentResponseDto.Message;
            versionResponseMessage.Version.Should().Be(NodeUtil.GetVersion());
        }
    }
}
