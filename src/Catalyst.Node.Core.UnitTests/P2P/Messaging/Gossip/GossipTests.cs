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

using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging.Gossip;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging.Gossip;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Transaction;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Serilog;
using SharpRepository.Repository;
using System;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P.Messaging.Gossip
{
    public sealed class GossipTests
    {
        private readonly IRepository<Peer> _peers;
        private readonly ILogger _logger;
        private readonly IPeerSettings _peerSettings;

        public GossipTests()
        {
            _logger = Substitute.For<ILogger>();
            _peers = Substitute.For<IRepository<Peer>>();
            _peerSettings = Substitute.For<IPeerSettings>();
            _peerSettings.BindAddress.Returns(IPAddress.Parse("127.0.0.1"));
            _peerSettings.Port.Returns(12543);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("2")]
        public void Gossip_Message_Test(string peerdId)
        {
            PopulatePeers(100);
            var cache = new MemoryCache(new MemoryCacheOptions());

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier(peerdId);
            var correlationId = Get_Gossip_Correlation_Id(peerIdentifier, cache);

            cache.TryGetValue(correlationId, out GossipRequest value);
            value.GossipCount.Should().Be((uint)Constants.MaxGossipPeersPerRound);
            value.ReceivedCount.Should().Be(1);
        }

        [Fact]
        public void Not_Enough_Peers_To_Gossip()
        {
            PopulatePeers(Constants.MaxGossipPeersPerRound - 1);
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var correlationId = Get_Gossip_Correlation_Id(peerIdentifier, cache);

            cache.TryGetValue(correlationId, out GossipRequest value);
            value.GossipCount.Should().Be((uint)Constants.MaxGossipPeersPerRound - 1);
            value.ReceivedCount.Should().Be(1);
        }

        [Fact]
        public void Gossip_Can_Execute_On_Handlers()
        {
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var recipientIdentifier = Substitute.For<IPeerIdentifier>();
            var gossipMessageHandler = Substitute.For<IGossipManager>();
            var fakeIp = IPAddress.Any;
            var guid = Guid.NewGuid();

            recipientIdentifier.Ip.Returns(fakeIp);
            recipientIdentifier.IpEndPoint.Returns(new IPEndPoint(fakeIp, 10));

            EmbeddedChannel channel = new EmbeddedChannel(
                new GossipHandler(gossipMessageHandler),
                new ObservableServiceHandler(_logger)
            );

            var transaction = new TransactionBroadcast();
            var anySigned = transaction.ToAnySigned(peerIdentifier.PeerId, guid)
               .ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid());

            channel.WriteInbound(anySigned);

            gossipMessageHandler.Received(Quantity.Exactly(1))
               .IncomingGossip(Arg.Any<ProtocolMessage>());
        }

        [Fact]
        public void Gossip_Can_Execute_Proto_Handler()
        {
            bool hasHitHandler = false;
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test");
            var handler = new TransactionBroadcastTestHandler(_logger, () => hasHitHandler = true);
            var manager = new GossipManager(peerIdentifier, _peers, Substitute.For<IMemoryCache>(), Substitute.For<IPeerClient>());
            var gossipHandler = new GossipHandler(manager);

            var protoDatagramChannelHandler = new ObservableServiceHandler(_logger);
            handler.StartObserving(protoDatagramChannelHandler.MessageStream);

            EmbeddedChannel channel = new EmbeddedChannel(gossipHandler, protoDatagramChannelHandler);

            var anySignedGossip = new TransactionBroadcast()
               .ToAnySigned(PeerIdHelper.GetPeerId(Guid.NewGuid().ToString()))
               .ToAnySigned(PeerIdHelper.GetPeerId(Guid.NewGuid().ToString()));

            channel.WriteInbound(anySignedGossip);
            hasHitHandler.Should().BeTrue();
        }

        [Fact]
        public void Can_Recognize_Gossip_Message()
        {
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var gossipMessage = new TransactionBroadcast().ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid())
               .ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid());
            gossipMessage.CheckIfMessageIsGossip().Should().BeTrue();

            var nonGossipMessage = new PingRequest().ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid());
            nonGossipMessage.CheckIfMessageIsGossip().Should().BeFalse();

            var secondNonGossipMessage = new PingRequest().ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid())
               .ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid());
            secondNonGossipMessage.CheckIfMessageIsGossip().Should().BeFalse();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(6)]
        [InlineData(3)]
        public void Gossip_Cache_Increased_Received_Count(int receivedCount)
        {
            PopulatePeers(100);
            var cache = new MemoryCache(new MemoryCacheOptions());

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var senderIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var messageFactory = new MessageFactory();
            IGossipManager gossipMessageHandler = new GossipManager(peerIdentifier, _peers, cache, Substitute.For<IPeerClient>());

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

            gossipMessageHandler.IncomingGossip(gossipDto);
            cache.TryGetValue(correlationId, out GossipRequest value);
            value.ReceivedCount.Should().Be(1);

            for (var i = 0; i < receivedCount; i++)
            {
                gossipMessageHandler.IncomingGossip(gossipDto);
            }

            cache.TryGetValue(correlationId, out value);
            value.ReceivedCount.Should().Be((uint)receivedCount + 1);
        }

        private Guid Get_Gossip_Correlation_Id(IPeerIdentifier peerIdentifier, IMemoryCache cache)
        {
            var messageFactory = new MessageFactory();
            var senderPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            var gossipMessageHandler = new
                GossipManager(senderPeerIdentifier, _peers, cache, Substitute.For<IPeerClient>());

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
            var peerIdentifiers = new List<Peer>();
            for (var i = 10; i < count + 10; i++)
            {
                peerIdentifiers.Add(new Peer
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

            public TransactionBroadcastTestHandler(ILogger logger, Action action) : base(logger)
            {
                _action = action;
            }

            protected override void Handler(IChanneledMessage<ProtocolMessage> message)
            {
                _action();
            }
        }
    }
}
