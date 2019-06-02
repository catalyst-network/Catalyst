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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Outbound;
using Catalyst.Protocol.Common;
using Dawn;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Catalyst.Common.IO.Messaging
{
    public class CorrelationManager : ICorrelationManager, IDisposable
    {
        private TimeSpan CacheTtl { get; }
        private readonly IMemoryCache _pendingRequests;
        private readonly MemoryCacheEntryOptions _entryOptions;
        private readonly ReplaySubject<IMessageEvictionEvent> _evictionEvent;
        public IObservable<IMessageEvictionEvent> EvictionEvents => _evictionEvent.AsObservable();
        
        public CorrelationManager(IMemoryCache cache,
            TimeSpan cacheTtl = default)
        {
            CacheTtl = cacheTtl == default ? Constants.CorrelationTtl : cacheTtl;
            _pendingRequests = cache;

            _evictionEvent = new ReplaySubject<IMessageEvictionEvent>(0);

            _entryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(CacheTtl).Token))
               .RegisterPostEvictionCallback(EvictionCallback);
        }
        
        private void EvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            //TODO: find a way to trigger the actual remove with correct reason
            //when the cache is not under pressure, eviction happens by token expiry :(
            //if (reason == EvictionReason.Removed) {return;}
            _evictionEvent.OnNext(new MessageEvictionEvent((IPeerIdentifier) key, value));
        }
        
        public void AddPendingRequest(PendingRequest pendingRequest)
        {
            _pendingRequests.Set(pendingRequest.Content.CorrelationId, pendingRequest, _entryOptions);
        }

        /// <summary>
        ///     Takes a generic request type of IMessage, and generic response type of IMessage and the message and look them up in the cache.
        ///     Return what's found or emit an-uncorrectable event 
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public bool TryMatchResponse(AnySigned response)
        {
            Guard.Argument(response, nameof(response)).NotNull();

            return _pendingRequests.TryGetValue(response.CorrelationId, out _);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _pendingRequests?.Dispose();   
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
