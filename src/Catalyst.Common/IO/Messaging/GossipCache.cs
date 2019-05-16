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
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Common.IO.Messaging
{
    public class GossipCache
        : MessageCorrelationCacheBase, IGossipCache
    {
        /// <summary>The peer discovery</summary>
        private readonly IPeerDiscovery _peerDiscovery;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>Initializes a new instance of the <see cref="GossipCache"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peerDiscovery">The peer discovery.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="logger">The logger.</param>
        public GossipCache(IPeerIdentifier peerIdentifier,
            IPeerDiscovery peerDiscovery,
            IMemoryCache cache,
            ILogger logger) : base(cache, logger, TimeSpan.FromMinutes(10))
        {
            _peerDiscovery = peerDiscovery;
            _peerIdentifier = peerIdentifier;
        }

        /// <inheritdoc/>
        public List<IPeerIdentifier> GetSortedPeers()
        {
            List<IPeerIdentifier> fullPeerList = new List<IPeerIdentifier>();
            fullPeerList.AddRange(_peerDiscovery.Peers.ToList());
            fullPeerList.Add(_peerIdentifier);
            var orderedList = fullPeerList.OrderBy(t => t.ToString()).ToList();
            return orderedList;
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
            var found = PendingRequests.TryGetValue(correlationId, out PendingRequest request);

            // Request does not exist, we can gossip this message
            if (!found)
            {
                return true;
            }

            return request.GossipCount < Constants.MaxGossipCount;
        }

        /// <inheritdoc/>
        public override TRequest TryMatchResponse<TRequest, TResponse>(AnySigned response)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public int GetCurrentPosition()
        {
            var sortedPeers = GetSortedPeers();
            var peerIdx = sortedPeers.IndexOf(_peerIdentifier);
            return peerIdx;
        }

        /// <inheritdoc/>
        public int GetGossipCount(Guid correlationId)
        {
            return GetPendingRequestValue(correlationId)?.GossipCount ?? -1;
        }

        /// <inheritdoc/>
        public void IncrementGossipCount(Guid correlationId, int updateCount)
        {
            var request = GetPendingRequestValue(correlationId);
            request.GossipCount += updateCount;
            AddPendingRequest(request);
        }

        /// <inheritdoc/>
        public override void AddPendingRequest(PendingRequest pendingRequest)
        {
            PendingRequests.Set(pendingRequest.Content.CorrelationId.ToGuid(), pendingRequest, EntryOptions);
        }

        /// <inheritdoc/>
        public void IncrementReceivedCount(Guid correlationId, int updateCount)
        {
            var request = GetPendingRequestValue(correlationId);
            if (request == null)
            {
                request = new PendingRequest
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

        /// <summary>Gets the pending request value.</summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns></returns>
        private PendingRequest GetPendingRequestValue(Guid guid)
        {
            PendingRequests.TryGetValue(guid, out PendingRequest request);
            return request;
        }
    }
}
