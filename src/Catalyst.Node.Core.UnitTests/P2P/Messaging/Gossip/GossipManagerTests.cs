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
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Serilog;
using SharpRepository.Repository;
using System;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.P2P.Messaging.Broadcast;
using Catalyst.Common.IO.Handlers;
using Catalyst.Node.Core.P2P.Messaging.Broadcast;
using SharpRepository.InMemoryRepository;
using Xunit;
using TransactionBroadcast = Catalyst.Protocol.Transaction.TransactionBroadcast;

namespace Catalyst.Node.Core.UnitTests.P2P.Messaging.Gossip
{
    public sealed class GossipManagerTests : IDisposable
    {
        private readonly IRepository<Peer> _peers;
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;

        public GossipManagerTests()
        {
            _logger = Substitute.For<ILogger>();
            _peers = new InMemoryRepository<Peer>();
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("2")]
        public async Task Gossip_Message_Test(string peerId)
        {
            PopulatePeers(100);
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier(peerId);
            var correlationId = await Get_Gossip_Correlation_Id();

            _cache.TryGetValue(correlationId, out BroadcastMessage value);
            value.GossipCount.Should().Be((uint) Constants.MaxGossipPeersPerRound);
            value.ReceivedCount.Should().Be(0);
        }

        [Fact]
        public async Task Not_Enough_Peers_To_Gossip()
        {
            PopulatePeers(Constants.MaxGossipPeersPerRound - 1);
            var correlationId = await Get_Gossip_Correlation_Id();

            _cache.TryGetValue(correlationId, out BroadcastMessage value);
            
            value.GossipCount.Should().Be((uint) Constants.MaxGossipPeersPerRound - 1);
            value.ReceivedCount.Should().Be(0);
        }

        [Fact]
        public async Task Gossip_Can_Execute_On_Handlers()
        {
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var recipientIdentifier = Substitute.For<IPeerIdentifier>();
            var gossipMessageHandler = Substitute.For<IBroadcastManager>();
            var fakeIp = IPAddress.Any;
            var guid = Guid.NewGuid();

            recipientIdentifier.Ip.Returns(fakeIp);
            recipientIdentifier.IpEndPoint.Returns(new IPEndPoint(fakeIp, 10));

            EmbeddedChannel channel = new EmbeddedChannel(
                new BroadcastHandler(gossipMessageHandler),
                new ObservableServiceHandler()
            );

            var transaction = new TransactionBroadcast();
            var anySigned = transaction.ToProtocolMessage(peerIdentifier.PeerId, guid)
               .ToProtocolMessage(peerIdentifier.PeerId, Guid.NewGuid());

            channel.WriteInbound(anySigned);

            await gossipMessageHandler.Received(Quantity.Exactly(1))
               .ReceiveAsync(Arg.Any<ProtocolMessage>());
        }

        [Fact]
        public async Task Gossip_Can_Execute_Proto_Handler()
        {
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test");
            var handler = new TestMessageObserver<TransactionBroadcast>(_logger);
            var manager = new BroadcastManager(peerIdentifier, _peers, Substitute.For<IMemoryCache>(), Substitute.For<IPeerClient>());
            var gossipHandler = new BroadcastHandler(manager);

            var protoDatagramChannelHandler = new ObservableServiceHandler();
            handler.StartObserving(protoDatagramChannelHandler.MessageStream);

            var channel = new EmbeddedChannel(gossipHandler, protoDatagramChannelHandler);

            var anySignedGossip = new TransactionBroadcast()
               .ToProtocolMessage(PeerIdHelper.GetPeerId(Guid.NewGuid().ToString()))
               .ToProtocolMessage(PeerIdHelper.GetPeerId(Guid.NewGuid().ToString()));

            channel.WriteInbound(anySignedGossip);
            void CheckHandlerTestAction() => handler.SubstituteObserver.Received(1).OnNext(Arg.Any<TransactionBroadcast>());

            await TaskHelper.WaitForAsync(() =>
            {
                try
                {
                    CheckHandlerTestAction();
                    return true;
                }
                catch (Exception)
                {
                    // ignored
                }

                return false;
            }, TimeSpan.FromSeconds(5));
            CheckHandlerTestAction();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(6)]
        [InlineData(3)]
        public async Task Gossip_Cache_Increased_Received_Count(int receivedCount)
        {
            PopulatePeers(100);

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var senderIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var messageFactory = new DtoFactory();
            IBroadcastManager broadcastMessageHandler = new BroadcastManager(peerIdentifier, _peers, _cache, Substitute.For<IPeerClient>());
            
            var messageDto = messageFactory.GetDto(
                TransactionHelper.GetTransaction(),
                peerIdentifier,
                senderIdentifier,
                Guid.NewGuid()
            );

            var gossipDto = messageDto.Message.ToProtocolMessage(senderIdentifier.PeerId, messageDto.CorrelationId);

            await broadcastMessageHandler.ReceiveAsync(gossipDto);
            await broadcastMessageHandler.BroadcastAsync(gossipDto);

            _cache.TryGetValue(messageDto.CorrelationId, out BroadcastMessage value);
            value.GossipCount.Should().Be((uint) Constants.MaxGossipPeersPerRound);
            value.ReceivedCount.Should().Be(1);

            for (var i = 0; i < receivedCount; i++)
            {
                await broadcastMessageHandler.ReceiveAsync(gossipDto);
            }

            _cache.TryGetValue(messageDto.CorrelationId, out value);
            value.ReceivedCount.Should().Be((uint) receivedCount + 1);
        }

        private async Task<Guid> Get_Gossip_Correlation_Id()
        {
            var senderPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            var gossipMessageHandler = new
                BroadcastManager(senderPeerIdentifier, _peers, _cache, Substitute.For<IPeerClient>());

            var gossipMessage = TransactionHelper.GetTransaction().ToProtocolMessage(senderPeerIdentifier.PeerId);
           
            await gossipMessageHandler.BroadcastAsync(gossipMessage);
            return gossipMessage.CorrelationId.ToGuid();
        }

        private void PopulatePeers(int count)
        {
            for (var i = 10; i < count + 10; i++)
            {
                _peers.Add(new Peer
                {
                    PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier(i.ToString())
                });
            }

            _peers.Count().Should().Be(count);
        }

        public void Dispose()
        {
            _cache.Dispose();
            _peers.Dispose();
        }
    }
}
