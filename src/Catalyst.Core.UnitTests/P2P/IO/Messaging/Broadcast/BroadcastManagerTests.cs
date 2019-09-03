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
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.P2P.Models;
using Catalyst.Core.P2P.Repository;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P.IO.Messaging.Broadcast
{
    public sealed class BroadcastManagerTests : IDisposable
    {
        private readonly IPeerRepository _peers;
        private readonly IMemoryCache _cache;
        private readonly IKeySigner _keySigner;
        private readonly IPeerIdentifier _senderPeerIdentifier;

        public BroadcastManagerTests()
        {
            _senderPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test");
            _keySigner = Substitute.For<IKeySigner>();
            var fakeSignature = Substitute.For<ISignature>();
            _keySigner.Sign(Arg.Any<byte[]>(), default).ReturnsForAnyArgs(fakeSignature);
            _peers = Substitute.For<IPeerRepository>();
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        [Fact]
        public async Task Can_Increase_Broadcast_Count_When_Broadcast_Owner_Broadcasting()
        {
            await TestBroadcast(100, _senderPeerIdentifier,
                BroadcastManager.BroadcastOwnerMaximumGossipPeersPerRound).ConfigureAwait(false);
        }

        [Fact]
        public async Task Can_Increase_Broadcast_Count_When_Broadcasting()
        {
            await TestBroadcast(100, PeerIdentifierHelper.GetPeerIdentifier("AnotherBroadcaster"),
                BroadcastManager.MaxGossipPeersPerRound).ConfigureAwait(false);
        }

        [Fact]
        public async Task Can_Broadcast_Message_When_Not_Enough_Peers_To_Gossip()
        {
            var peerCount = BroadcastManager.MaxGossipPeersPerRound - 1;
            await TestBroadcast(peerCount, _senderPeerIdentifier,
                peerCount).ConfigureAwait(false);
        }

        private async Task TestBroadcast(int peerCount, IPeerIdentifier broadcaster, int expectedBroadcastCount)
        {
            PopulatePeers(peerCount);
            var correlationId = await BroadcastMessage(broadcaster).ConfigureAwait(false);

            _cache.TryGetValue(correlationId.Id, out BroadcastMessage value);

            value.BroadcastCount.Should().Be((uint) expectedBroadcastCount);
            value.ReceivedCount.Should().Be(0);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(6)]
        [InlineData(3)]
        public async Task Can_Increase_Received_Count_When_Broadcast_Message_Is_Received(int receivedCount)
        {
            PopulatePeers(100);

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var senderIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            IBroadcastManager broadcastMessageHandler = new BroadcastManager(
                peerIdentifier, 
                _peers, 
                _cache, 
                Substitute.For<IPeerClient>(), 
                _keySigner,
                Substitute.For<ILogger>());

            var messageDto = new MessageDto(
                TransactionHelper.GetTransaction().ToProtocolMessage(senderIdentifier.PeerId),
                peerIdentifier
            );

            var gossipDto =
                new ProtocolMessageSigned
                {
                    Message = messageDto.Content.ToProtocolMessage(senderIdentifier.PeerId, messageDto.CorrelationId)
                };

            await broadcastMessageHandler.ReceiveAsync(gossipDto);

            _cache.TryGetValue(messageDto.CorrelationId.Id, out BroadcastMessage value);
            value.ReceivedCount.Should().Be(1);

            for (var i = 0; i < receivedCount; i++)
            {
                await broadcastMessageHandler.ReceiveAsync(gossipDto);
            }

            _cache.TryGetValue(messageDto.CorrelationId.Id, out value);
            value.ReceivedCount.Should().Be(receivedCount + 1);
        }

        private async Task<ICorrelationId> BroadcastMessage(IPeerIdentifier broadcaster)
        {
            var gossipMessageHandler = new
                BroadcastManager(_senderPeerIdentifier, 
                    _peers,
                    _cache, 
                    Substitute.For<IPeerClient>(), 
                    _keySigner, 
                    Substitute.For<ILogger>());

            var gossipMessage = TransactionHelper.GetTransaction().ToProtocolMessage(broadcaster.PeerId);

            await gossipMessageHandler.BroadcastAsync(gossipMessage);
            return gossipMessage.CorrelationId.ToCorrelationId();
        }

        private void PopulatePeers(int count)
        {
            var peerList = new List<Peer>();
            for (var i = 10; i < count + 10; i++)
            {
                var peer = new Peer
                {
                    PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier(i.ToString())
                };
                peerList.Add(peer);
                _peers.Get(peer.DocumentId).Returns(peer);
            }

            _peers.AsQueryable().Returns(peerList.AsQueryable());
        }

        public void Dispose()
        {
            _cache.Dispose();
            _peers.Dispose();
        }
    }
}
