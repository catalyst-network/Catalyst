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
using System.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.P2P.Messaging.Gossip;
using Catalyst.Node.Core.P2P.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Modules.Gossip
{
    public class GossipTests
    {
        private readonly IRepository<Peer> _peers;
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IReputableCache _messageCache;

        public GossipTests()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();

            _fakeContext.Channel.Returns(fakeChannel);
            _logger = Substitute.For<ILogger>();
            _peers = Substitute.For<IRepository<Peer>>();
            _messageCache = Substitute.For<IReputableCache>();
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

            cache.TryGetValue(correlationId, out GossipRequest value);
            value.GossipCount.Should().Be((uint) Constants.MaxGossipPeersPerRound);
            value.ReceivedCount.Should().Be(0);
        }

        [Fact]
        public void Not_Enough_Peers_To_Gossip()
        {
            PopulatePeers(Constants.MaxGossipPeersPerRound - 1);
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var correlationId = Get_Gossip_Correlation_Id(peerIdentifier, cache);

            cache.TryGetValue(correlationId, out GossipRequest value);
            value.GossipCount.Should().Be((uint) Constants.MaxGossipPeersPerRound - 1);
            value.ReceivedCount.Should().Be(0);
        }

        [Fact]
        public void Gossip_Can_Execute_On_Handlers()
        {
            var gossipMessageHandler = Substitute.For<IGossipMessageHandler>();
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var pingRequestHandler = new TestPingRequestGossipCorrelatableMessageHandler(
                gossipMessageHandler, _messageCache, _logger);

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
            var messageFactory = new P2PMessageFactory(_messageCache);
            var gossipCache = new GossipCache(_peers, cache, _logger);
            IGossipMessageHandler gossipMessageHandler = new GossipMessageHandler(gossipCache, messageFactory, peerIdentifier);

            var correlationId = Guid.NewGuid();

            var messageDto = messageFactory.GetMessage(
                new MessageDto(
                    new PingRequest(),
                    MessageTypes.Tell,
                    peerIdentifier,
                    PeerIdentifierHelper.GetPeerIdentifier("sender")
                ),
                correlationId
            );

            var channeledMessage = new ChanneledAnySigned(_fakeContext, messageDto);
            gossipMessageHandler.Handle(channeledMessage);
            cache.TryGetValue(correlationId, out GossipRequest value);
            value.GossipCount.Should().Be((uint) Constants.MaxGossipPeersPerRound);
            value.ReceivedCount.Should().Be(0);

            for (int i = 0; i < receivedCount; i++)
            {
                gossipMessageHandler.Handle(channeledMessage);
            }

            cache.TryGetValue(correlationId, out value);
            value.ReceivedCount.Should().Be((uint) receivedCount);
            value.GossipCount.Should().BeGreaterOrEqualTo((uint) Math.Min(gossipCache.GetMaxGossipCycles(correlationId),
                (value.ReceivedCount + 1) * Constants.MaxGossipPeersPerRound));
        }

        private Guid Get_Gossip_Correlation_Id(IPeerIdentifier peerIdentifier, IMemoryCache cache)
        {
            var gossipCache = new GossipCache(_peers, cache, _logger);
            var messageFactory = new P2PMessageFactory(_messageCache);
            var senderPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var gossipMessageHandler = new GossipMessageHandler(gossipCache, messageFactory, senderPeerIdentifier);

            var messageDto = messageFactory.GetMessage(
                new MessageDto(
                    new PingRequest(),
                    MessageTypes.Ask,
                    peerIdentifier,
                    senderPeerIdentifier
                )
            );
            var channeledMessage = new ChanneledAnySigned(_fakeContext, messageDto);

            gossipMessageHandler.Handle(channeledMessage);
            return messageDto.CorrelationId.ToGuid();
        }

        private void PopulatePeers(int count)
        {
            List<Peer> peerIdentifiers = new List<Peer>();
            for (int i = 10; i < count + 10; i++)
            {
                peerIdentifiers.Add(new Peer()
                {
                    PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier(i.ToString())
                });
            }

            _peers.GetAll().Returns(peerIdentifiers);
        }
        
        private sealed class TestPingRequestGossipCorrelatableMessageHandler : GossipResponseHandler<PingRequest, PingResponse, IMessageCorrelationCache>
        {
            public TestPingRequestGossipCorrelatableMessageHandler(IGossipMessageHandler gossipMessageHandler, IMessageCorrelationCache correlationCache, ILogger logger) : base(gossipMessageHandler, correlationCache, logger) { }
            protected override void Handler(IChanneledMessage<AnySigned> message) { }
            public override bool CanGossip(IChanneledMessage<AnySigned> message) { return true; }
        }
    }
}
