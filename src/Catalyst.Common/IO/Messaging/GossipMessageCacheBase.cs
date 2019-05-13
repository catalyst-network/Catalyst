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
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Dawn;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Common.IO.Messaging
{
    public class GossipMessageCacheBase
        : MessageCorrelationCacheBase, IGossipCacheBase
    {
        /// <summary>The maximum gossip count</summary>
        private const int MaxGossipCount = 10;

        private const int MaxPeersToGossip = 5;

        private IPeerDiscovery _peerDiscovery;

        protected GossipMessageCacheBase(IPeerDiscovery peerDiscovery,
            IMemoryCache cache,
            ILogger logger) : base(cache, logger, TimeSpan.FromMinutes(10))
        {
            this._peerDiscovery = peerDiscovery;
        }

        protected override PostEvictionDelegate GetInheritorDelegate()
        {
            Logger.Fatal("MessageCorrelationCache.GetInheritorDelegate() called without inheritor.");
            throw new NotImplementedException("Inheritors that uses the default constructor must implement the GetInheritorDelegate() method."); 
        }

        public bool CanGossip(AnySigned message)
        {
            var found = PendingRequests.TryGetValue(message.CorrelationId.ToGuid() + "-gossip", out PendingRequest request);

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

        public void Gossip(AnySigned message)
        {
            
        }

        public override TRequest TryMatchResponse<TRequest, TResponse>(AnySigned response)
        {
            Guard.Argument(response, nameof(response)).NotNull()
               .Require(r => typeof(TResponse).ShortenedProtoFullName().Equals(response.TypeUrl))
               .Require(r => typeof(TRequest).ShortenedProtoFullName().Equals(r.TypeUrl.GetRequestType()));

            var found = PendingRequests.TryGetValue(response.CorrelationId, out PendingRequest matched);
            matched.RecievedCount += 1;
            return !found ? null : matched.Content.FromAnySigned<TRequest>();
        }
    }
}
