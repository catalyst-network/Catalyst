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
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging.Gossip;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using SharpRepository.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Node.Core.P2P.Messaging.Gossip
{
    /// <summary>
    /// The Gossip Manager used to broadcast and receive gossip messages
    /// </summary>
    /// <seealso cref="IGossipManager" />
    public sealed class GossipManager : IGossipManager
    {
        /// <summary>The message factory</summary>
        private readonly IMessageFactory _messageFactory;

        /// <summary>The peers</summary>
        private readonly IRepository<Peer> _peers;

        /// <summary>The pending requests</summary>
        private readonly IMemoryCache _pendingRequests;

        /// <summary>The entry options</summary>
        private readonly MemoryCacheEntryOptions _entryOptions;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>The peer client</summary>
        private readonly IPeerClient _peerClient;

        /// <summary>Initializes a new instance of the <see cref="GossipManager"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peers">The peers.</param>
        /// <param name="memoryCache">The memory cache.</param>
        /// <param name="peerClient">The peer client.</param>
        public GossipManager(IPeerIdentifier peerIdentifier, IRepository<Peer> peers, IMemoryCache memoryCache, IPeerClient peerClient)
        {
            _peerIdentifier = peerIdentifier;
            _pendingRequests = memoryCache;
            _peers = peers;
            _peerClient = peerClient;
            _messageFactory = new MessageFactory();
            _entryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(10)).Token));
        }

        /// <inheritdoc/>
        public async Task BroadcastAsync(ProtocolMessage protocolMessage)
        {
            if (protocolMessage.CheckIfMessageIsGossip())
            {
                throw new NotSupportedException("Cannot broadcast a message which is already a gossip type");
            }

            var correlationId = protocolMessage.CorrelationId.ToGuid();
            var gossipRequest = await GetOrCreateAsync(correlationId).ConfigureAwait(false);
            var canGossip = CanGossip(gossipRequest);

            if (!canGossip)
            {
                return;
            }

            await SendGossipMessagesAsync(protocolMessage, gossipRequest).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task ReceiveAsync(ProtocolMessage protocolMessage)
        {
            if (!protocolMessage.CheckIfMessageIsGossip())
            {
                throw new NotSupportedException("The Message is not a gossip type");
            }

            var originalGossipedMessage = ProtocolMessage.Parser.ParseFrom(protocolMessage.Value);
            var correlationId = originalGossipedMessage.CorrelationId.ToGuid();
            var gossipRequest = await GetOrCreateAsync(correlationId).ConfigureAwait(false);
            gossipRequest.ReceivedCount += 1;
            UpdatePendingRequest(correlationId, gossipRequest);
        }

        /// <summary>Sends gossips to random peers.</summary>
        /// <param name="message">The message.</param>
        /// <param name="gossipRequest">The gossip request</param>
        private async Task SendGossipMessagesAsync(ProtocolMessage message, GossipRequest gossipRequest)
        {
            var peersToGossip = GetRandomPeers(Constants.MaxGossipPeersPerRound);
            var correlationId = message.CorrelationId.ToGuid();

            foreach (var peerIdentifier in peersToGossip)
            {
                var datagramEnvelope = _messageFactory.GetDatagramMessage(new MessageDto(message,
                    MessageTypes.Broadcast, peerIdentifier, _peerIdentifier), correlationId);
                await _peerClient.SendMessageAsync(datagramEnvelope).ConfigureAwait(false);
            }

            var updateCount = (uint) peersToGossip.Count;
            if (updateCount > 0)
            {
                gossipRequest.GossipCount += updateCount;
                UpdatePendingRequest(correlationId, gossipRequest);
            }
        }

        /// <summary>Gets the random peers.</summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private List<IPeerIdentifier> GetRandomPeers(int count)
        {
            var peers = _peers.GetAll().Shuffle();
            var peerAmount = Math.Min(peers.Count, count);
            return peers.Select(x => x.PeerIdentifier).Take(peerAmount).ToList();
        }

        /// <summary>Determines whether this instance can gossip the specified correlation identifier.</summary>
        /// <param name="request">The gossip request</param>
        /// <returns><c>true</c> if this instance can gossip the specified correlation identifier; otherwise, <c>false</c>.</returns>
        private bool CanGossip(GossipRequest request)
        {
            return request.GossipCount < GetMaxGossipCycles(request);
        }
        
        /// <summary>Adds the gossip request.</summary>
        /// <param name="gossipRequest">The gossip request.</param>
        /// <param name="correlationId">The message correlation ID</param>
        private void UpdatePendingRequest(Guid correlationId, GossipRequest gossipRequest)
        {
            _pendingRequests.Set(correlationId, gossipRequest, _entryOptions);
        }

        /// <summary>Gets the maximum gossip cycles.</summary>
        /// <param name="gossipRequest"></param>
        /// <returns></returns>
        private uint GetMaxGossipCycles(GossipRequest gossipRequest)
        {
            var peerNetworkSize = gossipRequest.PeerNetworkSize;
            return (uint) (Math.Log(Math.Max(10, peerNetworkSize) / (double) Constants.MaxGossipPeersPerRound) /
                Math.Max(1, gossipRequest.GossipCount / Constants.MaxGossipPeersPerRound));
        }
        
        /// <summary>Increments the received count.</summary>
        /// <param name="correlationId">The correlation identifier.</param>
        private async Task<GossipRequest> GetOrCreateAsync(Guid correlationId)
        {
            var request = await _pendingRequests.GetOrCreateAsync(correlationId, async entry =>
            {
                entry.SetOptions(_entryOptions);
                var gossipRequest = await Task.FromResult(new GossipRequest
                {
                    ReceivedCount = 0,
                    GossipCount = 0,
                    PeerNetworkSize = _peers.GetAll().Count()
                }).ConfigureAwait(false);
                entry.Value = gossipRequest;
                return gossipRequest;
            }).ConfigureAwait(false);

            return request;
        }
    }
}
