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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Protocol.Common;
using Dawn;
using Microsoft.Extensions.Caching.Memory;

namespace Catalyst.Common.IO.Messaging.Correlation
{
    /// <inheritdoc cref="IMessageCorrelationManager"/>
    /// <summary>
    /// In this implementation of the correlation manager, the underlying cache adds records
    /// with a Time To Live after which they get automatically get deleted from the cache (inflicting
    /// a reputation penalty for the peer who didn't reply).
    /// </summary>
    public sealed class MessageCorrelationManager : IMessageCorrelationManager, IDisposable
    {
        private readonly IMemoryCache _pendingRequests;
        private readonly Func<MemoryCacheEntryOptions> _entryOptions;
        private readonly ReplaySubject<IMessageEvictionEvent> _evictionEvent;
        public IObservable<IMessageEvictionEvent> EvictionEvents => _evictionEvent.AsObservable();
        
        public MessageCorrelationManager(IMemoryCache cache,
            IChangeTokenProvider changeTokenProvider)
        {
            _pendingRequests = cache;

            _evictionEvent = new ReplaySubject<IMessageEvictionEvent>(0);

            _entryOptions = () => new MemoryCacheEntryOptions()
               .AddExpirationToken(changeTokenProvider.GetChangeToken())
               .RegisterPostEvictionCallback(EvictionCallback);
        }
        
        private void EvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            //TODO: find a way to trigger the actual remove with correct reason
            //when the cache is not under pressure, eviction happens by token expiry :(
            //if (reason == EvictionReason.Removed) {return;}
            var message = (CorrelatableMessage) value;
            _evictionEvent.OnNext(new MessageEvictionEvent(message));
        }
        
        /// <inheritdoc />
        public void AddPendingRequest(CorrelatableMessage correlatableMessage)
        {
            _pendingRequests.Set(correlatableMessage.Content.CorrelationId, correlatableMessage, _entryOptions());
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
