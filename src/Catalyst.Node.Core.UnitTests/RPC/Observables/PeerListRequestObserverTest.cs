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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.RPC.IO.Observables;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC.Observables
{
    /// <summary>
    /// Tests the peer list CLI and RPC calls
    /// </summary>
    public sealed class PeerListRequestObserverTest
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerListRequestObserverTest"/> class.
        /// </summary>
        public PeerListRequestObserverTest()
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
        public async Task TestPeerListRequestResponse(params string[] fakePeers)
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
            
            var messageFactory = new DtoFactory();

            var requestMessage = messageFactory.GetDto(
                new GetPeerListRequest(),
                PeerIdentifierHelper.GetPeerIdentifier("sender"),
                PeerIdentifierHelper.GetPeerIdentifier("recipient")
            );
            
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, requestMessage.Message.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId));

            var handler = new PeerListRequestObserver(PeerIdentifierHelper.GetPeerIdentifier("sender"), _logger, peerRepository);
            handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<GetPeerListResponse>) receivedCalls[0].GetArguments().Single();
            
            sentResponseDto.Message.GetType()
               .Should()
               .BeAssignableTo<GetPeerListResponse>();

            var responseContent = sentResponseDto.FromIMessageDto();

            responseContent.Peers.Count.Should().Be(fakePeers.Length);
        }
    }
}
