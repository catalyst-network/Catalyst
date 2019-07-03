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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Protocol.Common;
using Dawn;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace Catalyst.Common.IO.Messaging.Correlation
{
    public abstract class MessageCorrelationManagerBase : IMessageCorrelationManager
    {
        public TimeSpan CacheTtl { get; }
        protected readonly IMemoryCache PendingRequests;
        protected readonly ILogger Logger;
        private readonly MemoryCacheEntryOptions _entryOptions;

        protected MessageCorrelationManagerBase(IMemoryCache cache,
            ILogger logger,
            TimeSpan cacheTtl = default)
        {
            CacheTtl = cacheTtl == default ? TimeSpan.FromSeconds(10) : cacheTtl;
            PendingRequests = cache;
            Logger = logger;

            _entryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(CacheTtl).Token))
               .RegisterPostEvictionCallback(EvictionCallback);
        }

        /// <summary>
        ///     A callback method that is called upon eviction of a message,
        ///     for basic protocols such the RPC, it doesn't do anything
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="reason"></param>
        /// <param name="state"></param>
        protected abstract void EvictionCallback(object key, object value, EvictionReason reason, object state);

        /// <summary>
        ///     Stores a CorrelatableMessage in the cache so we can correlate incoming messages.
        /// </summary>
        /// <param name="correlatableMessage"></param>
        public virtual void AddPendingRequest(CorrelatableMessage correlatableMessage)
        {
            PendingRequests.Set(correlatableMessage.Content.CorrelationId, correlatableMessage, _entryOptions);
        }

        /// <summary>
        ///     Takes a generic request type of IMessage, and generic response type of IMessage and the message and look them up in the cache.
        ///     Return what's found or emit an-uncorrectable event 
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public virtual bool TryMatchResponse(ProtocolMessage response)
        {
            Guard.Argument(response, nameof(response)).NotNull();

            return PendingRequests.TryGetValue(response.CorrelationId, out _);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            PendingRequests?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
