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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.IO.Messaging.Broadcast;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Common.Repository;
using Catalyst.Core.Lib.P2P.IO.Messaging.Broadcast;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using SharpRepository.InMemoryRepository;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P.IO.Messaging.Broadcast
{
    public sealed class BroadcastManagerTests : IDisposable
    {
        private readonly IPeerRepository _peers;
        private readonly IMemoryCache _cache;
        private IKeySigner _keySigner;

        public BroadcastManagerTests()
        {
            _keySigner = Substitute.For<IKeySigner>();
            _keySigner.Sign(Arg.Any<byte[]>()).Returns(new Signature(new byte[64], new byte[32]));
            _peers = Substitute.For<IPeerRepository>();
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        [Fact]
        public async Task Can_Increase_Broadcast_Count_When_Broadcasting()
        {
            PopulatePeers(100);
            var correlationId = await BroadcastMessage();

            _cache.TryGetValue(correlationId.Id, out BroadcastMessage value);
            value.BroadcastCount.Should().Be((uint) Constants.MaxGossipPeersPerRound);
            value.ReceivedCount.Should().Be(0);
        }

        [Fact]
        public async Task Can_Broadcast_Message_When_Not_Enough_Peers_To_Gossip()
        {
            PopulatePeers(Constants.MaxGossipPeersPerRound - 1);
            var correlationId = await BroadcastMessage();

            _cache.TryGetValue(correlationId.Id, out BroadcastMessage value);

            value.BroadcastCount.Should().Be((uint) Constants.MaxGossipPeersPerRound - 1);
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
            var messageFactory = new DtoFactory();

            IBroadcastManager broadcastMessageHandler = new BroadcastManager(peerIdentifier, _peers, _cache, Substitute.For<IPeerClient>(), _keySigner);

            var messageDto = messageFactory.GetDto(
                TransactionHelper.GetTransaction(),
                peerIdentifier,
                senderIdentifier,
                CorrelationId.GenerateCorrelationId()
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
            value.ReceivedCount.Should().Be((uint) receivedCount + 1);
        }

        private async Task<ICorrelationId> BroadcastMessage()
        {
            var senderPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            var gossipMessageHandler = new
                BroadcastManager(senderPeerIdentifier, _peers, _cache, Substitute.For<IPeerClient>(), _keySigner);

            var gossipMessage = TransactionHelper.GetTransaction().ToProtocolMessage(senderPeerIdentifier.PeerId);

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
