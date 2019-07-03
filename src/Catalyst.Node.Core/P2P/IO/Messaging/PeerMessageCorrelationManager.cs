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
using System.Threading;
using Catalyst.Common.Interfaces.P2P.IO.Messaging;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.IO.Messaging;
using Catalyst.Protocol.Common;
using Dawn;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Catalyst.Node.Core.P2P.IO.Messaging
{
    public class PeerMessageCorrelationManager : IPeerMessageCorrelationManager
    {
        public TimeSpan CacheTtl { get; }
        private readonly IReputationManager _reputationManager;
        private readonly IMemoryCache _pendingRequests;
        private readonly MemoryCacheEntryOptions _entryOptions;

        public PeerMessageCorrelationManager(IReputationManager reputationManager,
            IMemoryCache cache,
            TimeSpan cacheTtl = default)
        {
            CacheTtl = cacheTtl == default ? TimeSpan.FromSeconds(10) : cacheTtl;
            _reputationManager = reputationManager;
            _pendingRequests = cache;

            _entryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(CacheTtl).Token))
               .RegisterPostEvictionCallback(EvictionCallback);
        }

        private void EvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            //TODO: find a way to trigger the actual remove with correct reason
            //when the cache is not under pressure, eviction happens by token expiry :(
            //if (reason == EvictionReason.Removed) {return;}
            var message = (CorrelatableMessage) value;
            _reputationManager.ReputationEvents.OnNext(new MessageEvictionEvent(message));
        }
        
        public void AddPendingRequest(CorrelatableMessage correlatableMessage)
        {
            _pendingRequests.Set(correlatableMessage.Content.CorrelationId, correlatableMessage, _entryOptions);
        }

        /// <summary>
        ///     Takes a generic request type of IMessage, and generic response type of IMessage and the message and look them up in the cache.
        ///     Return what's found or emit an-uncorrectable event 
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public bool TryMatchResponse(ProtocolMessage response)
        {
            Guard.Argument(response, nameof(response)).NotNull();

            return _pendingRequests.TryGetValue(response.CorrelationId, out _);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _pendingRequests?.Dispose();
            _evictionEvent?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
