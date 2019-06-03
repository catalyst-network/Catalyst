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
using Catalyst.Common.Interfaces.IO.Messaging.Gossip;
using Catalyst.Common.Interfaces.P2P;
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

        /// <summary>The peer client factory</summary>
        private readonly IPeerClientFactory _peerClientFactory;

        /// <summary>The peers</summary>
        private readonly IRepository<Peer> _peers;

        /// <summary>The pending requests</summary>
        private readonly IMemoryCache _pendingRequests;

        /// <summary>The entry options</summary>
        private readonly MemoryCacheEntryOptions _entryOptions;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>Initializes a new instance of the <see cref="GossipManager"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peers">The peers.</param>
        /// <param name="memoryCache">The memory cache.</param>
        /// <param name="peerClientFactory">The peer client factory.</param>
        public GossipManager(IPeerIdentifier peerIdentifier, IRepository<Peer> peers, IMemoryCache memoryCache, IPeerClientFactory peerClientFactory)
        {
            _peerIdentifier = peerIdentifier;
            _peerClientFactory = peerClientFactory;
            _pendingRequests = memoryCache;
            _peers = peers;
            _messageFactory = new MessageFactory();
            _entryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(10)).Token));
        }

        /// <inheritdoc/>
        public void Broadcast(AnySigned anySigned)
        {
            if (anySigned.CheckIfMessageIsGossip())
            {
                throw new NotSupportedException("Cannot broadcast a message which is already a gossip type");
            }

            Gossip(anySigned);
        }

        /// <inheritdoc/>
        public void IncomingGossip(AnySigned anySigned)
        {
            if (!anySigned.CheckIfMessageIsGossip())
            {
                throw new NotSupportedException("The Message is not a gossip type");
            }

            // TODO: Check Gossip inner signature and outer signature
            AnySigned originalGossipedMessage = AnySigned.Parser.ParseFrom(anySigned.Value);
            IncrementReceivedCount(originalGossipedMessage.CorrelationId.ToGuid(), 1);
        }

        /// <summary>Gossips the specified message.</summary>
        /// <param name="message">The message.</param>
        private void Gossip(AnySigned message)
        {
            var correlationId = message.CorrelationId.ToGuid();
            var gossipCount = GetGossipCount(correlationId);
            var canGossip = CanGossip(correlationId);
            bool isInCache = gossipCount != -1;

            if (!canGossip)
            {
                return;
            }

            if (!isInCache)
            {
                var request = new GossipRequest
                {
                    SentAt = DateTime.Now,
                    Recipient = new PeerIdentifier(message.PeerId),
                    Content = message,
                    ReceivedCount = 1,
                };
                AddPendingRequest(correlationId, request);
            }

            SendGossipMessages(message);
        }

        /// <summary>Sends gossips to random peers.</summary>
        /// <param name="message">The message.</param>
        private void SendGossipMessages(AnySigned message)
        {
            var peersToGossip = GetRandomPeers(Constants.MaxGossipPeersPerRound);
            var correlationId = message.CorrelationId.ToGuid();

            foreach (var peerIdentifier in peersToGossip)
            {
                var datagramEnvelope = _messageFactory.GetDatagramMessage(new MessageDto(message,
                    MessageTypes.Gossip, peerIdentifier, _peerIdentifier), correlationId);
                 _peerClientFactory.Client.SendMessage(datagramEnvelope);
            }

            var updateCount = (uint) peersToGossip.Count;
            if (updateCount > 0)
            {
                IncrementGossipCount(correlationId, updateCount);
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
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns><c>true</c> if this instance can gossip the specified correlation identifier; otherwise, <c>false</c>.</returns>
        private bool CanGossip(Guid correlationId)
        {
            var request = GetPendingRequestValue(correlationId);

            // Request does not exist, we can gossip this message
            if (request == null)
            {
                return true;
            }

            return request.GossipCount < GetMaxGossipCycles(correlationId);
        }

        /// <summary>Gets the amount of times a message has been gossiped</summary>
        /// <param name="correlationId">The message correlation identifier.</param>
        /// <returns></returns>
        public int GetGossipCount(Guid correlationId)
        {
            var gossipCount = (int?) GetPendingRequestValue(correlationId)?.GossipCount;
            return gossipCount ?? -1;
        }

        /// <inheritdoc/>
        private void IncrementGossipCount(Guid correlationId, uint updateCount)
        {
            var request = GetPendingRequestValue(correlationId);
            request.GossipCount += updateCount;
            AddPendingRequest(correlationId, request);
        }

        /// <summary>Adds the gossip request.</summary>
        /// <param name="gossipRequest">The gossip request.</param>
        /// <param name="correlationId">The message correlation ID</param>
        private void AddPendingRequest(Guid correlationId, GossipRequest gossipRequest)
        {
            if (GetGossipCount(correlationId) == -1)
            {
                gossipRequest.PeerNetworkSize = _peers.GetAll().Count();
            }

            _pendingRequests.Set(correlationId, gossipRequest, _entryOptions);
        }

        /// <summary>Increments the received count.</summary>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="updateCount">The amount to increment by.</param>
        private void IncrementReceivedCount(Guid correlationId, uint updateCount)
        {
            var request = GetPendingRequestValue(correlationId);
            if (request == null)
            {
                request = new GossipRequest
                {
                    ReceivedCount = updateCount
                };
            }
            else
            {
                request.ReceivedCount += updateCount;
            }

            AddPendingRequest(correlationId, request);
        }

        /// <summary>Gets the maximum gossip cycles.</summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns></returns>
        private uint GetMaxGossipCycles(Guid guid)
        {
            var peerNetworkSize = GetPendingRequestValue(guid)?.PeerNetworkSize ?? _peers.GetAll().Count();
            return (uint) (Math.Log(peerNetworkSize / (double) Constants.MaxGossipPeersPerRound) /
                Math.Max(1, 2 * GetGossipCount(guid) / Constants.MaxGossipPeersPerRound));
        }

        /// <summary>Gets the pending request value.</summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns></returns>
        private GossipRequest GetPendingRequestValue(Guid guid)
        {
            _pendingRequests.TryGetValue(guid, out GossipRequest request);
            return request;
        }
    }
}
