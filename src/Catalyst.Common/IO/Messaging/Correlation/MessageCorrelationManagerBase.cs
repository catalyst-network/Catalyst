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
    public abstract class MessageCorrelationManagerBase : IMessageCorrelationManager, IDisposable
    {
        private readonly IMemoryCache _pendingRequests;
        private readonly Func<MemoryCacheEntryOptions> _entryOptions;

        protected MessageCorrelationManagerBase(IMemoryCache cache,
            IChangeTokenProvider changeTokenProvider)
        {
            _pendingRequests = cache;
            
            _entryOptions = () => new MemoryCacheEntryOptions()
               .AddExpirationToken(changeTokenProvider.GetChangeToken())
               .RegisterPostEvictionCallback(EvictionCallback);
        }

        protected abstract void EvictionCallback(object key, object value, EvictionReason reason, object state);
        
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _pendingRequests?.Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
