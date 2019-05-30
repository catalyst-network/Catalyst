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
using System.Net;
using System.Reactive.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Gossip;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.P2P.Messaging.Gossip;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Transaction;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using FluentAssertions.Common;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P
{
    public class GossipTests
    {
        private readonly IRepository<Peer> _peers;
        private readonly ILogger _logger;
        private readonly IReputableCache _messageCache;
        private readonly IPeerSettings _peerSettings;

        public GossipTests()
        {
            _logger = Substitute.For<ILogger>();
            _peers = Substitute.For<IRepository<Peer>>();
            _messageCache = Substitute.For<IReputableCache>();
            _peerSettings = Substitute.For<IPeerSettings>();
            _peerSettings.BindAddress.Returns(IPAddress.Any);
            _peerSettings.Port.Returns(10);
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

            cache.TryGetValue(correlationId.ToByteString(), out GossipRequest value);
            value.GossipCount.Should().Be((uint) Constants.MaxGossipPeersPerRound);
            value.ReceivedCount.Should().Be(1);
        }

        [Fact]
        public void Not_Enough_Peers_To_Gossip()
        {
            PopulatePeers(Constants.MaxGossipPeersPerRound - 1);
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var correlationId = Get_Gossip_Correlation_Id(peerIdentifier, cache);

            cache.TryGetValue(correlationId.ToByteString(), out GossipRequest value);
            value.GossipCount.Should().Be((uint) Constants.MaxGossipPeersPerRound - 1);
            value.ReceivedCount.Should().Be(1);
        }

        [Fact]
        public void Gossip_Can_Execute_On_Handlers()
        {
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var recipientIdentifier = Substitute.For<IPeerIdentifier>();
            var messageFactory = new P2PMessageFactory(_messageCache);
            var gossipMessageHandler = Substitute.For<IGossipManager>();
            var fakeIp = IPAddress.Any;
            var guid = Guid.NewGuid();

            recipientIdentifier.Ip.Returns(fakeIp);
            recipientIdentifier.IpEndPoint.Returns(new IPEndPoint(fakeIp, 10));

            EmbeddedChannel channel = new EmbeddedChannel(
                new ProtoDatagramChannelHandler(),
                new GossipHandler(gossipMessageHandler)
            );

            var transaction = new TransactionBroadcast();
            var anySigned = transaction.ToAnySigned(peerIdentifier.PeerId, guid);

            channel.WriteInbound(messageFactory.GetMessageInDatagramEnvelope(
                new MessageDto(anySigned, MessageTypes.Gossip, recipientIdentifier, peerIdentifier)));

            gossipMessageHandler.Received(Quantity.Exactly(1))
               .IncomingGossip(Arg.Any<AnySigned>());
        }

        [Fact]
        public void Gossip_Can_Execute_Proto_Handler()
        {
            var manager = new GossipManager(
                PeerIdentifierHelper.GetPeerIdentifier("Test"), _messageCache, Substitute.For<IGossipCache>(), _peerSettings);
            var gossipHandler = new GossipHandler(manager);
            var protoDatagramChannelHandler = new ProtoDatagramChannelHandler();

            var allMessageStream = protoDatagramChannelHandler.MessageStream.Merge(gossipHandler.MessageStream);

            bool hasHitHandler = false;

            var handler = new TransactionBroadcastTestHandler(_logger, () => hasHitHandler = true);
            handler.StartObserving(allMessageStream);

            EmbeddedChannel channel = new EmbeddedChannel(protoDatagramChannelHandler, gossipHandler);

            var anySignedGossip = new TransactionBroadcast()
               .ToAnySigned(PeerIdHelper.GetPeerId(Guid.NewGuid().ToString()))
               .ToAnySigned(PeerIdHelper.GetPeerId(Guid.NewGuid().ToString()));

            var gossipMessage = anySignedGossip.ToDatagram(new IPEndPoint(IPAddress.Any, 5050));
            channel.WriteInbound(gossipMessage);
            hasHitHandler.IsSameOrEqualTo(true);
        }

        [Fact]
        public void Can_Recognize_Gossip_Message()
        {
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var gossipMessage = new TransactionBroadcast().ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid())
               .ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid());
            gossipMessage.CheckIfMessageIsGossip().IsSameOrEqualTo(true);

            var nonGossipMessage = new PingRequest().ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid());
            nonGossipMessage.CheckIfMessageIsGossip().IsSameOrEqualTo(false);

            var secondNonGossipMessage = new PingRequest().ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid())
               .ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid());
            secondNonGossipMessage.CheckIfMessageIsGossip().IsSameOrEqualTo(false);
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
            var senderIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var messageFactory = new P2PMessageFactory(_messageCache);
            var gossipCache = new GossipCache(_peers, cache, _logger);
            IGossipManager gossipMessageHandler = new GossipManager(peerIdentifier, _messageCache, gossipCache, _peerSettings);

            var correlationId = Guid.NewGuid();

            var messageDto = messageFactory.GetMessage(
                new MessageDto(
                    new TransactionBroadcast(),
                    MessageTypes.Tell,
                    peerIdentifier,
                    senderIdentifier
                ),
                correlationId
            );

            var gossipDto = messageDto.ToAnySigned(senderIdentifier.PeerId, correlationId);

            gossipMessageHandler.Broadcast(messageDto);
            cache.TryGetValue(correlationId.ToByteString(), out GossipRequest value);
            value.GossipCount.Should().Be((uint) Constants.MaxGossipPeersPerRound);
            value.ReceivedCount.Should().Be(1);

            for (int i = 0; i < receivedCount; i++)
            {
                gossipMessageHandler.IncomingGossip(gossipDto);
                gossipMessageHandler.Broadcast(messageDto);
            }

            cache.TryGetValue(correlationId.ToByteString(), out value);
            value.ReceivedCount.Should().Be((uint) receivedCount + 1);
            value.GossipCount.Should().BeGreaterOrEqualTo((uint) Math.Min(gossipCache.GetMaxGossipCycles(correlationId),
                value.ReceivedCount * Constants.MaxGossipPeersPerRound));
        }

        private Guid Get_Gossip_Correlation_Id(IPeerIdentifier peerIdentifier, IMemoryCache cache)
        {
            var gossipCache = new GossipCache(_peers, cache, _logger);
            var messageFactory = new P2PMessageFactory(_messageCache);
            var senderPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var gossipMessageHandler = new GossipManager(senderPeerIdentifier, _messageCache, gossipCache, _peerSettings);

            var messageDto = messageFactory.GetMessage(
                new MessageDto(
                    new PingRequest(),
                    MessageTypes.Ask,
                    peerIdentifier,
                    senderPeerIdentifier
                )
            );

            gossipMessageHandler.Broadcast(messageDto);
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

        internal class TransactionBroadcastTestHandler : MessageHandlerBase<TransactionBroadcast>,
            IP2PMessageHandler
        {
            private readonly Action _action;

            public TransactionBroadcastTestHandler(ILogger logger, Action action) : base(logger) { _action = action; }

            protected override void Handler(IChanneledMessage<AnySigned> message) { _action(); }
        }
    }
}
