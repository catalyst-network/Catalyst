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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Common.Rpc;
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
    /// Tests the peer list CLI and RPC calls
    /// </summary>
    public sealed class PeerListRequestHandlerTest
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;

        private IRpcCorrelationCache _subbedCorrelationCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerListRequestHandlerTest"/> class.
        /// </summary>
        public PeerListRequestHandlerTest()
        {
            _subbedCorrelationCache = Substitute.For<IRpcCorrelationCache>();
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        /// <summary>
        /// Tests the peer list request and response.
        /// </summary>
        /// <param name="fakePeers">The fake peers.</param>
        [Theory]
        [InlineData("FakePeer1", "FakePeer2")]
        [InlineData("FakePeer1002", "FakePeer6000", "FakePeerSataoshi")]
        public void TestPeerListRequestResponse(params string[] fakePeers)
        {
            var peerRepository = Substitute.For<IRepository<Peer>>();
            var peerList = new List<Peer>();

            fakePeers.ToList().ForEach(fakePeer =>
            {
                peerList.Add(new Peer
                {
                    Reputation = 0,
                    LastSeen = DateTime.Now,
                    PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier(fakePeer)
                });
            });

            // Let peerRepository return the fake peer list
            peerRepository.GetAll().Returns(peerList.ToArray());

            // Build a fake remote endpoint
            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));
            
            var rpcMessageFactory = new RpcMessageFactory(_subbedCorrelationCache);
            var sendPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            var requestMessage = rpcMessageFactory.GetMessage(new MessageDto(
                new GetPeerListRequest(),
                MessageTypes.Ask,
                PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                sendPeerIdentifier
            ));
            
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, requestMessage);

            var handler = new PeerListRequestHandler(sendPeerIdentifier, _logger, peerRepository, rpcMessageFactory);
            handler.StartObserving(messageStream);

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponse = (AnySigned) receivedCalls[0].GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(GetPeerListResponse.Descriptor.ShortenedFullName());

            var responseContent = sentResponse.FromAnySigned<GetPeerListResponse>();

            responseContent.Peers.Count.Should().Be(fakePeers.Length);
        }
    }
}
