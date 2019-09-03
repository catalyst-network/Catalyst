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
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Extensions;
using Catalyst.Core.Network;
using Catalyst.Core.P2P.Models;
using Catalyst.Core.P2P.Repository;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Rpc.IO.Observers
{
    /// <summary>
    /// Tests the peer list CLI and RPC calls
    /// </summary>
    public sealed class PeerListRequestObserverTests
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;

        /// <summary>
        /// Initializes a new instance of the <see>
        ///     <cref>PeerListRequestObserverTest</cref>
        /// </see>
        /// class.
        /// </summary>
        public PeerListRequestObserverTests()
        {
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
#pragma warning disable 1998
        public async Task TestPeerListRequestResponse(params string[] fakePeers)
#pragma warning restore 1998
        {
            var testScheduler = new TestScheduler();
            var peerRepository = Substitute.For<IPeerRepository>();
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
            
            var protocolMessage = new GetPeerListRequest().ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId);
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, testScheduler, protocolMessage);

            var handler = new PeerListRequestObserver(PeerIdentifierHelper.GetPeerIdentifier("sender"), _logger, peerRepository);
            handler.StartObserving(messageStream);

            testScheduler.Start();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls[0].GetArguments().Single();
            
            var responseContent = sentResponseDto.Content.FromProtocolMessage<GetPeerListResponse>();

            responseContent.Peers.Count.Should().Be(fakePeers.Length);
        }
    }
}
