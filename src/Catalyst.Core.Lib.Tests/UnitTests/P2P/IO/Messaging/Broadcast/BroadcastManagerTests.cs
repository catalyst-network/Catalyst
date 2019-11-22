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
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Messaging.Broadcast
{
    public sealed class BroadcastManagerTests : IDisposable
    {
        private readonly IPeerRepository _peers;
        private readonly IMemoryCache _cache;
        private readonly IKeySigner _keySigner;
        private readonly PeerId _senderPeerId;
        private readonly IPeerSettings _peerSettings;

        public BroadcastManagerTests()
        {
            _senderPeerId = PeerIdHelper.GetPeerId("sender");
            _keySigner = Substitute.For<IKeySigner>();
            var fakeSignature = Substitute.For<ISignature>();
            _keySigner.Sign(Arg.Any<byte[]>(), default).ReturnsForAnyArgs(fakeSignature);
            _keySigner.CryptoContext.SignatureLength.Returns(64);
            _peers = new PeerRepository(new InMemoryRepository<Peer, string>());
            _cache = new MemoryCache(new MemoryCacheOptions());
            _peerSettings = _senderPeerId.ToSubstitutedPeerSettings();
        }

        [Fact]
        public async Task Can_Increase_Broadcast_Count_When_Broadcast_Owner_Broadcasting()
        {
            await TestBroadcast(100, _senderPeerId,
                BroadcastManager.BroadcastOwnerMaximumGossipPeersPerRound).ConfigureAwait(false);
        }

        [Fact]
        public async Task Can_Increase_Broadcast_Count_When_Broadcasting()
        {
            await TestBroadcast(100,
                PeerIdHelper.GetPeerId("AnotherBroadcaster"),
                BroadcastManager.MaxGossipPeersPerRound).ConfigureAwait(false);
        }

        [Fact]
        public async Task Can_Broadcast_Message_When_Not_Enough_Peers_To_Gossip()
        {
            var peerCount = BroadcastManager.MaxGossipPeersPerRound - 1;
            await TestBroadcast(peerCount, _senderPeerId,
                peerCount).ConfigureAwait(false);
        }

        private async Task TestBroadcast(int peerCount, PeerId broadcaster, int expectedBroadcastCount)
        {
            PopulatePeers(peerCount);
            var correlationId = await BroadcastMessage(broadcaster)
               .ConfigureAwait(false);

            _cache.TryGetValue(correlationId.Id, out BroadcastMessage value);

            value.BroadcastCount.Should().Be((uint)expectedBroadcastCount);
            value.ReceivedCount.Should().Be(0);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(6)]
        [InlineData(3)]
        public async Task Can_Increase_Received_Count_When_Broadcast_Message_Is_Received(int receivedCount)
        {
            PopulatePeers(100);

            var peerId = PeerIdHelper.GetPeerId("1");
            var senderIdentifier = PeerIdHelper.GetPeerId("sender");

            IBroadcastManager broadcastMessageHandler = new BroadcastManager(
                _peers,
                _peerSettings,
                _cache,
                Substitute.For<IPeerClient>(),
                _keySigner,
                Substitute.For<ILogger>());

            var messageDto = new MessageDto(
                TransactionHelper.GetPublicTransaction().ToProtocolMessage(senderIdentifier),
                peerId
            );

            var gossipDto = messageDto.Content
               .ToProtocolMessage(senderIdentifier, messageDto.CorrelationId);

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

        private async Task<ICorrelationId> BroadcastMessage(PeerId broadcaster)
        {
            var gossipMessageHandler = new
                BroadcastManager(
                    _peers,
                    _peerSettings,
                    _cache,
                    Substitute.For<IPeerClient>(),
                    _keySigner,
                    Substitute.For<ILogger>());

            var innerMessage = TransactionHelper.GetPublicTransaction()
               .ToProtocolMessage(broadcaster);

            await gossipMessageHandler.BroadcastAsync(innerMessage);
            return innerMessage.CorrelationId.ToCorrelationId();
        }

        private void PopulatePeers(int count)
        {
            _peers.Add(Enumerable.Range(10, count + 10).Select(i => new Peer
            {
                PeerId = PeerIdHelper.GetPeerId(i.ToString())
            }));
        }

        public void Dispose()
        {
            _cache.Dispose();
            _peers.Dispose();
        }
    }
}
