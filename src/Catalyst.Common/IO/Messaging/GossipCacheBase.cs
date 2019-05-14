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
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Common.IO.Messaging
{
    public class GossipCacheBase<TMessage> 
        : MessageCorrelationCacheBase, IGossipCacheBase<TMessage>
        where TMessage : class, IMessage<TMessage>
    {
        /// <summary>The maximum gossip count</summary>
        private const int MaxGossipCount = 10;
        
        /// <summary>The peer discovery</summary>
        private readonly IPeerDiscovery _peerDiscovery;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>Initializes a new instance of the <see cref="GossipCacheBase{TMessage}"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peerDiscovery">The peer discovery.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="logger">The logger.</param>
        protected GossipCacheBase(IPeerIdentifier peerIdentifier,
            IPeerDiscovery peerDiscovery,
            IMemoryCache cache,
            ILogger logger) : base(cache, logger, TimeSpan.FromMinutes(10))
        {
            this._peerDiscovery = peerDiscovery;
            this._peerIdentifier = peerIdentifier;
        }

        /// <inheritdoc/>
        public override void AddPendingRequest(PendingRequest pendingRequest)
        {
            PendingRequests.Set(pendingRequest.Content.CorrelationId.ToGuid() + "gossip", pendingRequest, EntryOptions);
        }

        /// <inheritdoc/>
        protected override PostEvictionDelegate GetInheritorDelegate()
        {
            Logger.Fatal("MessageCorrelationCache.GetInheritorDelegate() called without inheritor.");
            throw new NotImplementedException("Inheritors that uses the default constructor must implement the GetInheritorDelegate() method.");
        }

        /// <inheritdoc/>
        public bool CanGossip(Guid correlationId)
        {
            var found = PendingRequests.TryGetValue(correlationId + "gossip", out PendingRequest request);

            // Request does not exist, we can gossip this message
            if (!found)
            {
                return true;
            }
            else
            {
                if (request.GossipCount < MaxGossipCount)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override TRequest TryMatchResponse<TRequest, TResponse>(AnySigned response)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public int GetCurrentPosition()
        {
            List<IPeerIdentifier> fullPeerList = new List<IPeerIdentifier>();
            fullPeerList.AddRange(_peerDiscovery.Peers.ToArray());
            fullPeerList.Add(_peerIdentifier);
            fullPeerList.Sort();
            int peerIdx = fullPeerList.IndexOf(_peerIdentifier);
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
            PendingRequest request = GetPendingRequestValue(correlationId);
            request.GossipCount += updateCount;
            AddPendingRequest(request);
        }

        /// <inheritdoc/>
        public void IncrementReceivedCount(Guid correlationId, int updateCount)
        {
            PendingRequest request = GetPendingRequestValue(correlationId);
            if (request == null)
            {
                request = new PendingRequest()
                {
                    RecievedCount = updateCount
                };
            }
            else
            {
                request.RecievedCount += updateCount;
            }

            AddPendingRequest(request);
        }

        /// <summary>Gets the pending request value.</summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns></returns>
        private PendingRequest GetPendingRequestValue(Guid guid)
        {
            PendingRequests.TryGetValue(guid + "gossip", out PendingRequest request);
            return request;
        }
    }
}
