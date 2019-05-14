using System.Collections.Concurrent;
using System.Collections.Generic;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging;
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
        public const int PeerCount = 100;

        private readonly IPeerDiscovery _peerDiscovery;
        private readonly ILogger _logger;
        private IChannelHandlerContext _fakeContext;
        private IChannel _fakeChannel;

        public GossipTests()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _fakeChannel = Substitute.For<IChannel>();

            _fakeContext.Channel.Returns(_fakeChannel);
            _logger = Substitute.For<ILogger>();
            _peerDiscovery = Substitute.For<IPeerDiscovery>();
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("2")]
        public void Gossip_Message_Test(string peerdId)
        {
            PopulatePeers(PeerCount);
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var peerIdentifier = new PeerIdentifier(PeerIdHelper.GetPeerId(peerdId));
            string correlationId = Get_Gossip_Correlation_Id(peerIdentifier, cache);

            cache.TryGetValue(correlationId, out PendingRequest value);
            value.GossipCount.Should().Be(Constants.MaxGossipPeers);
            value.RecievedCount.Should().Be(1);
        }

        [Fact]
        public void Not_Enough_Peers_To_Gossip()
        {
            PopulatePeers(3);
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var peerIdentifier = new PeerIdentifier(PeerIdHelper.GetPeerId("1"));
            string correlationId = Get_Gossip_Correlation_Id(peerIdentifier, cache);

            cache.TryGetValue(correlationId, out PendingRequest value);
            value.GossipCount.Should().Be(3);
            value.RecievedCount.Should().Be(1);
        }

        private string Get_Gossip_Correlation_Id(IPeerIdentifier peerIdentifier, IMemoryCache cache)
        {
            var gossipCache = new GossipCacheBase(peerIdentifier, _peerDiscovery, cache, _logger);
            var messageFactory = new P2PMessageFactory<PingRequest>();
            var gossipMessageHandler = new GossipMessageHandler<PingRequest>(peerIdentifier, _peerDiscovery, gossipCache, messageFactory);

            var messageDto = messageFactory.GetMessage(
                new PingRequest(),
                peerIdentifier,
                new PeerIdentifier(PeerIdHelper.GetPeerId("sender")),
                MessageTypes.Ask
            );
            var channeledMessage = new ChanneledAnySigned(_fakeContext, messageDto);

            gossipMessageHandler.StartGossip(channeledMessage);
            return messageDto.CorrelationId.ToGuid() + "gossip";
        }

        private void PopulatePeers(int count)
        {
            List<IPeerIdentifier> peerIdentifiers = new List<IPeerIdentifier>();
            for (int i = 10; i < count + 10; i++)
            {
                peerIdentifiers.Add(new PeerIdentifier(PeerIdHelper.GetPeerId(i.ToString())));
            }

            var peerCollection = new ConcurrentStack<IPeerIdentifier>(peerIdentifiers);
            _peerDiscovery.Peers.Returns(peerCollection);
        }
    }
}
