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
using System.Threading;
using System.Threading.Tasks;
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
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.P2P.Messaging.Gossip;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels;
using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.P2P
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
            /*var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var recipientIdentifier = Substitute.For<IPeerIdentifier>();
            var messageFactory = new P2PMessageFactory(_messageCache);
            var gossipMessageHandler = Substitute.For<IGossipManager>();
            var serverSettings = Substitute.For<IPeerSettings>();
            var serverIp = IPAddress.Parse("127.0.0.1");
            var port = 42065;
            var guid = Guid.NewGuid();

            serverSettings.BindAddress.Returns(serverIp);
            serverSettings.Port.Returns(port);
            recipientIdentifier.Ip.Returns(serverIp);
            recipientIdentifier.Port.Returns(port);
            recipientIdentifier.IpEndPoint.Returns(new IPEndPoint(serverIp, port));

            gossipMessageHandler
               .CheckIfMessageIsGossip(Arg.Any<IChanneledMessage<AnySigned>>())
               .Returns(true);

            using (UdpServer server = new P2PService(serverSettings, Substitute.For<IPeerDiscovery>(),
                gossipMessageHandler, new List<IP2PMessageHandler>()))
            {
                server.Bootstrap(Substitute.For<IChannelHandler>(), serverIp, port);

                using (UdpClient client = new PeerClient(new IPEndPoint(serverIp, port), new List<IP2PMessageHandler>(),
                    gossipMessageHandler))
                {
                    var pingRequest = new PingRequest();
                    var anySigned = pingRequest.ToAnySigned(peerIdentifier.PeerId, guid);

                    client.Channel.WriteAndFlushAsync(messageFactory.GetMessageInDatagramEnvelope(
                            new MessageDto(anySigned, MessageTypes.Gossip, recipientIdentifier, peerIdentifier)))
                       .GetAwaiter().GetResult();
                    Thread.Sleep(500);
                    gossipMessageHandler.Received(Quantity.Exactly(1))
                       .IncomingGossip(Arg.Any<IChanneledMessage<AnySigned>>());
                    client.Channel.CloseAsync();
                    server.Channel.CloseAsync();
                }
            }*/
        }

        [Fact]
        public void Can_Recognize_Gossip_Message()
        {
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var gossipCache = new GossipCache(_peers, cache, _logger);
            IGossipManager gossipMessageHandler = new GossipManager(peerIdentifier, _messageCache, gossipCache);
            var gossipMessage = new PingRequest().ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid())
               .ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid());
            gossipMessageHandler
               .CheckIfMessageIsGossip(new ChanneledAnySigned(_fakeContext, gossipMessage))
               .IsSameOrEqualTo(true);

            var nonGossipMessage = new PingRequest().ToAnySigned(peerIdentifier.PeerId, Guid.NewGuid());
            gossipMessageHandler
               .CheckIfMessageIsGossip(new ChanneledAnySigned(_fakeContext, nonGossipMessage))
               .IsSameOrEqualTo(false);
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
            IGossipManager gossipMessageHandler = new GossipManager(peerIdentifier, _messageCache, gossipCache);

            var correlationId = Guid.NewGuid();

            var messageDto = messageFactory.GetMessage(
                new MessageDto(
                    new PingRequest(),
                    MessageTypes.Tell,
                    peerIdentifier,
                    senderIdentifier
                ),
                correlationId
            ).ToAnySigned(senderIdentifier.PeerId, correlationId);

            var channeledMessage = new ChanneledAnySigned(_fakeContext, messageDto);
            gossipMessageHandler.Broadcast(channeledMessage);
            cache.TryGetValue(correlationId.ToByteString(), out GossipRequest value);
            value.GossipCount.Should().Be((uint) Constants.MaxGossipPeersPerRound);
            value.ReceivedCount.Should().Be(1);

            for (int i = 0; i < receivedCount; i++)
            {
                gossipMessageHandler.IncomingGossip(channeledMessage);
                gossipMessageHandler.Broadcast(channeledMessage);
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
            var gossipMessageHandler = new GossipManager(senderPeerIdentifier, _messageCache, gossipCache);

            var messageDto = messageFactory.GetMessage(
                new MessageDto(
                    new PingRequest(),
                    MessageTypes.Ask,
                    peerIdentifier,
                    senderPeerIdentifier
                )
            );
            var channeledMessage = new ChanneledAnySigned(_fakeContext, messageDto);

            gossipMessageHandler.Broadcast(channeledMessage);
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
    }
}
