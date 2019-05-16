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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.P2P.Messaging.Handlers;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Modules.Gossip
{
    public class GossipTests
    {
        private readonly IPeerDiscovery _peerDiscovery;
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;

        public GossipTests()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();

            _fakeContext.Channel.Returns(fakeChannel);
            _logger = Substitute.For<ILogger>();
            _peerDiscovery = Substitute.For<IPeerDiscovery>();
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("2")]
        public void Gossip_Message_Test(string peerdId)
        {
            PopulatePeers(100);
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier(peerdId);
            var correlationId = Get_Gossip_Correlation_Id(peerIdentifier, cache);

            cache.TryGetValue(correlationId, out PendingRequest value);
            value.GossipCount.Should().Be(Constants.MaxGossipPeers);
            value.ReceivedCount.Should().Be(0);
        }

        [Fact]
        public void Not_Enough_Peers_To_Gossip_Circular_List_Goes_Round()
        {
            PopulatePeers(Constants.MaxGossipPeers - 1);
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var correlationId = Get_Gossip_Correlation_Id(peerIdentifier, cache);

            cache.TryGetValue(correlationId, out PendingRequest value);
            value.GossipCount.Should().Be(Constants.MaxGossipPeers);
            value.ReceivedCount.Should().Be(0);
        }

        [Fact]
        public void Gossip_Can_Execute_On_Handlers()
        {
            var gossipMessageHandler = Substitute.For<IGossipMessageHandler>();
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");

            var pingRequestHandler = new PingRequestHandler(peerIdentifier, _logger)
            {
                GossipHandler = gossipMessageHandler
            };

            var pingRequest = new PingRequest().ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid());
            var message = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, pingRequest);
            pingRequestHandler.StartObserving(message);
            gossipMessageHandler.ReceivedWithAnyArgs(1).Handle(default);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(6)]
        [InlineData(3)]
        public void Gossip_Cache_Increased_Received_Count(int receivedCount)
        {
            PopulatePeers(100);
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var messageFactory = new P2PMessageFactory<PingRequest>();
            var gossipCache = new GossipCache(peerIdentifier, _peerDiscovery, cache, _logger);
            var gossipMessageHandler = new GossipMessageHandler<PingRequest>(peerIdentifier, gossipCache, messageFactory);
            var correlationId = Guid.NewGuid();

            var messageDto = messageFactory.GetMessage(
                new PingRequest(),
                peerIdentifier,
                PeerIdentifierHelper.GetPeerIdentifier("sender"),
                MessageTypes.Tell,
                correlationId
            );

            var channeledMessage = new ChanneledAnySigned(_fakeContext, messageDto);
            gossipMessageHandler.Handle(channeledMessage);
            cache.TryGetValue(correlationId, out PendingRequest value);
            value.GossipCount.Should().Be(Constants.MaxGossipPeers);
            value.ReceivedCount.Should().Be(0);

            for (int i = 0; i < receivedCount; i++)
            {
                gossipMessageHandler.Handle(channeledMessage);
            }

            cache.TryGetValue(correlationId, out value);
            value.ReceivedCount.Should().Be(receivedCount);
            value.GossipCount.Should().Be(Math.Min(Constants.MaxGossipCount,
                value.ReceivedCount * Constants.MaxGossipCount));
        }

        private Guid Get_Gossip_Correlation_Id(IPeerIdentifier peerIdentifier, IMemoryCache cache)
        {
            var gossipCache = new GossipCache(peerIdentifier, _peerDiscovery, cache, _logger);
            var messageFactory = new P2PMessageFactory<PingRequest>();
            var gossipMessageHandler = new GossipMessageHandler<PingRequest>(peerIdentifier, gossipCache, messageFactory);

            var messageDto = messageFactory.GetMessage(
                new PingRequest(),
                peerIdentifier,
                PeerIdentifierHelper.GetPeerIdentifier("sender"),
                MessageTypes.Ask
            );
            var channeledMessage = new ChanneledAnySigned(_fakeContext, messageDto);

            gossipMessageHandler.Handle(channeledMessage);
            return messageDto.CorrelationId.ToGuid();
        }

        private void PopulatePeers(int count)
        {
            List<IPeerIdentifier> peerIdentifiers = new List<IPeerIdentifier>();
            for (int i = 10; i < count + 10; i++)
            {
                peerIdentifiers.Add(PeerIdentifierHelper.GetPeerIdentifier(i.ToString()));
            }

            var peerCollection = new ConcurrentStack<IPeerIdentifier>(peerIdentifiers);
            _peerDiscovery.Peers.Returns(peerCollection);
        }
    }
}
