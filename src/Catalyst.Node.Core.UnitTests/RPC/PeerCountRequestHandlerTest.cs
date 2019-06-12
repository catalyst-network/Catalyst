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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC
{
    /// <summary>
    /// Tests the peer count CLI and RPC calls
    /// </summary>
    public sealed class PeerCountRequestHandlerTest
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PeerListRequestHandlerTest"/> class.
        /// </summary>
        public PeerCountRequestHandlerTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        /// <summary>
        /// Tests the peer count request and response.
        /// </summary>
        /// <param name="fakePeers">The peer count.</param>
        [Theory]
        [InlineData(40)]
        [InlineData(20)]
        public async Task TestPeerListRequestResponse(int fakePeers)
        {
            var peerRepository = Substitute.For<IRepository<Peer>>();
            var peerList = new List<Peer>();

            for (var i = 0; i < fakePeers; i++)
            {
                peerList.Add(new Peer
                {
                    Reputation = 0,
                    LastSeen = DateTime.Now,
                    PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier(i.ToString())
                });
            }
            
            // Build a fake remote endpoint
            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));

            peerRepository.GetAll().Returns(peerList);

            var messageFactory = new MessageFactory();
            var sendPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            var requestMessage = messageFactory.GetMessage(new MessageDto(
                new GetPeerCountRequest(),
                MessageTypes.Request,
                PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                sendPeerIdentifier
            ));

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, requestMessage);

            var handler = new PeerCountRequestHandler(sendPeerIdentifier, peerRepository, messageFactory, _logger);
            handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolScheduler();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponse = (ProtocolMessage) receivedCalls[0].GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(GetPeerCountResponse.Descriptor.ShortenedFullName());

            var responseContent = sentResponse.FromProtocolMessage<GetPeerCountResponse>();

            responseContent.PeerCount.Should().Be(fakePeers);
        }
    }
}
