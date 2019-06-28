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
using Catalyst.Common.Interfaces.P2P.Messaging.Broadcast;
using Catalyst.Common.IO.Messaging;
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
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging.Dto;

namespace Catalyst.Node.Core.P2P.Messaging.Broadcast
{
    /// <summary>
    /// The Gossip Manager used to broadcast and receive gossip messages
    /// </summary>
    /// <seealso cref="IBroadcastManager" />
    public sealed class BroadcastManager : IBroadcastManager
    {
        /// <summary>The message factory</summary>
        private readonly IDtoFactory _dtoFactory;

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

        /// <summary>Initializes a new instance of the <see cref="BroadcastManager"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peers">The peers.</param>
        /// <param name="memoryCache">The memory cache.</param>
        /// <param name="peerClient">The peer client.</param>
        public BroadcastManager(IPeerIdentifier peerIdentifier, IRepository<Peer> peers, IMemoryCache memoryCache, IPeerClient peerClient)
        {
            _peerIdentifier = peerIdentifier;
            _pendingRequests = memoryCache;
            _peers = peers;
            _peerClient = peerClient;
            _dtoFactory = new DtoFactory();
            _entryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(10)).Token));
        }

        /// <inheritdoc/>
        public async Task BroadcastAsync(ProtocolMessage protocolMessage)
        {
            if (protocolMessage.CheckIfMessageIsBroadcast())
            {
                throw new NotSupportedException("Cannot broadcast a message which is already a gossip type");
            }

            var correlationId = protocolMessage.CorrelationId.ToGuid();
            var gossipRequest = await GetOrCreateAsync(correlationId).ConfigureAwait(false);

            if (!CanGossip(gossipRequest))
            {
                return;
            }

            SendGossipMessages(protocolMessage, gossipRequest);
        }

        /// <inheritdoc/>
        public async Task ReceiveAsync(ProtocolMessage protocolMessage)
        {
            var correlationId = protocolMessage.CorrelationId.ToGuid();
            var gossipRequest = await GetOrCreateAsync(correlationId).ConfigureAwait(false);
            gossipRequest.ReceivedCount += 1;
            UpdatePendingRequest(correlationId, gossipRequest);
        }

        /// <summary>Sends gossips to random peers.</summary>
        /// <param name="message">The message.</param>
        /// <param name="broadcastMessage">The gossip request</param>
        private void SendGossipMessages(ProtocolMessage message, BroadcastMessage broadcastMessage)
        {
            try
            {
                var peersToGossip = GetRandomPeers(Constants.MaxGossipPeersPerRound);
                var correlationId = message.CorrelationId.ToGuid();

                foreach (var peerIdentifier in peersToGossip)
                {
                    _peerClient.SendMessage(_dtoFactory.GetDto(message, 
                        peerIdentifier, 
                        _peerIdentifier,
                        correlationId)
                    );
                }

                var updateCount = (uint) peersToGossip.Count;
                if (updateCount <= 0)
                {
                    return;
                }

                broadcastMessage.BroadcastCount += updateCount;
                UpdatePendingRequest(correlationId, broadcastMessage);
            }
            catch (Exception e)
            {
                //@TODO log
            }
        }

        /// <summary>Gets the random peers.</summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private List<IPeerIdentifier> GetRandomPeers(int count)
        {
            return _peers
               .AsQueryable()
               .Select(c => c.PkId).Shuffle().Take(count).Select(_peers.Get).Select(p => p.PeerIdentifier).ToList();
        }

        /// <summary>Determines whether this instance can gossip the specified correlation identifier.</summary>
        /// <param name="request">The gossip request</param>
        /// <returns><c>true</c> if this instance can gossip the specified correlation identifier; otherwise, <c>false</c>.</returns>
        private bool CanGossip(BroadcastMessage request)
        {
            return request.BroadcastCount < GetMaxGossipCycles(request);
        }

        /// <summary>Adds the gossip request.</summary>
        /// <param name="broadcastMessage">The gossip request.</param>
        /// <param name="correlationId">The message correlation ID</param>
        private void UpdatePendingRequest(Guid correlationId, BroadcastMessage broadcastMessage)
        {
            _pendingRequests.Set(correlationId, broadcastMessage, _entryOptions);
        }

        /// <summary>Gets the maximum gossip cycles.</summary>
        /// <param name="broadcastMessage"></param>
        /// <returns></returns>
        private uint GetMaxGossipCycles(BroadcastMessage broadcastMessage)
        {
            var peerNetworkSize = broadcastMessage.PeerNetworkSize;
            return (uint) (Math.Log(Math.Max(10, peerNetworkSize) / (double) Constants.MaxGossipPeersPerRound) /
                Math.Max(1, broadcastMessage.BroadcastCount / Constants.MaxGossipPeersPerRound));
        }

        /// <summary>Increments the received count.</summary>
        /// <param name="correlationId">The correlation identifier.</param>
        private async Task<BroadcastMessage> GetOrCreateAsync(Guid correlationId)
        {
            var request = await _pendingRequests.GetOrCreateAsync(correlationId, async entry =>
            {
                entry.SetOptions(_entryOptions);
                var gossipRequest = await Task.FromResult(new BroadcastMessage
                {
                    ReceivedCount = 0,
                    BroadcastCount = 0,
                    PeerNetworkSize = _peers.GetAll().Count()
                }).ConfigureAwait(false);
                entry.Value = gossipRequest;
                return gossipRequest;
            }).ConfigureAwait(false);

            return request;
        }
    }
}
