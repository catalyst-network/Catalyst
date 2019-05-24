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

using System.Linq;
using System.Net;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Rpc;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Util;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC
{
    public sealed class GetVersionRequestHandlerTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private IRpcCorrelationCache _subbedCorrelationCache;

        public GetVersionRequestHandlerTest()
        {
            _subbedCorrelationCache = Substitute.For<IRpcCorrelationCache>();
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();

            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
            _fakeContext.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));
        }

        [Fact]
        public void GetVersion_UsingValidRequest_ShouldSendVersionResponse()
        {
            var rpcMessageFactory = new RpcMessageFactory(_subbedCorrelationCache);
            var request = new RpcMessageFactory(_subbedCorrelationCache).GetMessage(new MessageDto(
                new VersionRequest(),
                MessageTypes.Ask,
                PeerIdentifierHelper.GetPeerIdentifier("recepient"),
                PeerIdentifierHelper.GetPeerIdentifier("sender")));

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            var subbedCache = Substitute.For<IRpcCorrelationCache>();
            var handler = new GetVersionRequestHandler(PeerIdentifierHelper.GetPeerIdentifier("sender"), _logger, subbedCache, rpcMessageFactory);
            handler.StartObserving(messageStream);

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count().Should().Be(1);

            var sentResponse = (AnySigned) receivedCalls.Single().GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(VersionResponse.Descriptor.ShortenedFullName());

            var responseContent = sentResponse.FromAnySigned<VersionResponse>();
            responseContent.Version.Should().Be(NodeUtil.GetVersion());
        }
    }
}
