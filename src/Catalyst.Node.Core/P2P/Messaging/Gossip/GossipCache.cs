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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.Interfaces.IO.Messaging.Gossip;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.P2P;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P.Messaging.Gossip
{
    public sealed class GossipCache
        : MessageCorrelationCacheBase, IGossipCache
    {
        /// <summary>The peers</summary>
        private readonly IRepository<Peer> _peers;

        /// <summary>Initializes a new instance of the <see cref="GossipCache"/> class.</summary>
        /// <param name="peers">The peers.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="logger">The logger.</param>
        public GossipCache(IRepository<Peer> peers,
            IMemoryCache cache,
            ILogger logger) : base(cache, logger, TimeSpan.FromMinutes(10))
        {
            _peers = peers;
        }

        /// <inheritdoc/>
        public List<IPeerIdentifier> GetRandomPeers(int count)
        {
            List<IPeerIdentifier> randomPeers = new List<IPeerIdentifier>();
            var peers = this._peers.GetAll().ToList();
            var peerAmount = Math.Min(peers.Count, count);
            for (int i = 0; i < peerAmount; i++)
            {
                randomPeers.Add(peers.RandomElement().PeerIdentifier);
            }

            return randomPeers;
        }

        /// <inheritdoc/>
        protected override PostEvictionDelegate GetInheritorDelegate()
        {
            return ChangeReputationOnEviction;
        }

        private void ChangeReputationOnEviction(object key, object value, EvictionReason reason, object state)
        {
            // we don't having anything to really do here for gossip.
        }

        /// <inheritdoc/>
        public bool CanGossip(Guid correlationId)
        {
            var request = GetPendingRequestValue(correlationId);

            // Request does not exist, we can gossip this message
            if (request == null)
            {
                return true;
            }

            return request.GossipCount < GetMaxGossipCycles(correlationId);
        }
        
        /// <inheritdoc/>
        public int GetGossipCount(Guid correlationId)
        {
            var gossipCount = (int?) GetPendingRequestValue(correlationId)?.GossipCount;
            return gossipCount ?? -1;
        }

        /// <inheritdoc/>
        public void IncrementGossipCount(Guid correlationId, uint updateCount)
        {
            var request = GetPendingRequestValue(correlationId);
            request.GossipCount += updateCount;
            AddPendingRequest(request);
        }

        /// <inheritdoc cref="IGossipCache"/>
        public override void AddPendingRequest(PendingRequest pendingRequest)
        {
            var guid = pendingRequest.Content.CorrelationId.ToGuid();

            if (GetGossipCount(guid) == -1)
            {
                ((GossipRequest) pendingRequest).PeerNetworkSize = _peers.GetAll().Count();
            }

            base.AddPendingRequest(pendingRequest);
        }

        /// <inheritdoc/>
        public void IncrementReceivedCount(Guid correlationId, uint updateCount)
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

            AddPendingRequest(request);
        }

        /// <inheritdoc/>
        public uint GetMaxGossipCycles(Guid guid)
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
            PendingRequests.TryGetValue(guid.ToByteString(), out GossipRequest request);
            return request;
        }
    }
}
