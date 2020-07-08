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
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using Catalyst.TestUtils.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using NUnit.Framework;
using Catalyst.Core.Lib.P2P.Repository;
using MultiFormats;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Messaging.Broadcast
{
    public sealed class BroadcastManagerTests : IDisposable
    {
        private IPeerRepository _peers;
        private IMemoryCache _cache;
        private FakeKeySigner _keySigner;
        private MultiAddress _sender;
        private IPeerSettings _peerSettings;

        [SetUp]
        public void Init()
        {
            _sender = MultiAddressHelper.GetAddress("sender");
            _keySigner = Substitute.For<FakeKeySigner>();
            var fakeSignature = Substitute.For<ISignature>();
            _keySigner.Sign(Arg.Any<byte[]>(), default).ReturnsForAnyArgs(fakeSignature);
            _keySigner.CryptoContext.SignatureLength.Returns(64);
            _peers = new PeerRepository(new InMemoryRepository<Peer, string>());
            _cache = new MemoryCache(new MemoryCacheOptions());
            _peerSettings = _sender.ToSubstitutedPeerSettings();
        }

        [Test]
        public async Task Can_Increase_Broadcast_Count_When_Broadcast_Owner_Broadcasting()
        {
            await TestBroadcast(100, _sender,
                BroadcastManager.BroadcastOwnerMaximumGossipPeersPerRound).ConfigureAwait(false);
        }

        [Test]
        public async Task Can_Increase_Broadcast_Count_When_Broadcasting()
        {
            await TestBroadcast(100,
                MultiAddressHelper.GetAddress("AnotherBroadcaster"),
                BroadcastManager.MaxGossipPeersPerRound).ConfigureAwait(false);
        }

        [Test]
        public async Task Can_Broadcast_Message_When_Not_Enough_Peers_To_Gossip()
        {
            var peerCount = BroadcastManager.MaxGossipPeersPerRound - 1;
            await TestBroadcast(peerCount, _sender,
                peerCount).ConfigureAwait(false);
        }

        private async Task TestBroadcast(int peerCount, MultiAddress broadcaster, int expectedBroadcastCount)
        {
            PopulatePeers(peerCount);
            var correlationId = await BroadcastMessage(broadcaster)
               .ConfigureAwait(false);

            _cache.TryGetValue(correlationId.Id, out BroadcastMessage value);

            value.BroadcastCount.Should().Be((uint) expectedBroadcastCount);
            value.ReceivedCount.Should().Be(0);
        }

        [Theory]
        [TestCase(1)]
        [TestCase(6)]
        [TestCase(3)]
        public async Task Can_Increase_Received_Count_When_Broadcast_Message_Is_Received(int receivedCount)
        {
            PopulatePeers(100);

            var peerId = MultiAddressHelper.GetAddress("1");

            IBroadcastManager broadcastMessageHandler = new BroadcastManager(
                _peers,
                _peerSettings,
                _cache,
                Substitute.For<IPeerClient>(),
                _keySigner,
                Substitute.For<ILogger>());

            var messageDto = new MessageDto(
                TransactionHelper.GetPublicTransaction().ToProtocolMessage(_sender),
                peerId
            );

            var gossipDto = messageDto.Content
               .ToProtocolMessage(_sender, messageDto.CorrelationId);

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

        private async Task<ICorrelationId> BroadcastMessage(MultiAddress broadcaster)
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
            for (var i = 10; i < count + 10; i++)
            {
                _peers.Add(new Peer
                {
                    Address = MultiAddressHelper.GetAddress(i.ToString())
                });
            }
        }

        public void Dispose()
        {
            _cache.Dispose();
            _peers.Dispose();
        }
    }
}
