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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Protocol.Common;
using Dawn;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Common.IO.Messaging.Correlation
{
    public abstract class MessageCorrelationManagerBase : IMessageCorrelationManager
    {
        protected readonly IMemoryCache PendingRequests;
        private readonly Func<MemoryCacheEntryOptions> _entryOptions;
        protected readonly ILogger Logger;

        protected MessageCorrelationManagerBase(IMemoryCache cache,
            ILogger logger,
            IChangeTokenProvider changeTokenProvider)
        {
            PendingRequests = cache;
            Logger = logger;

            _entryOptions = () => new MemoryCacheEntryOptions()
               .AddExpirationToken(changeTokenProvider.GetChangeToken())
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
        public virtual void AddPendingRequest(CorrelatableMessage<ProtocolMessage> correlatableMessage)
        {
            PendingRequests.Set(correlatableMessage.Content.CorrelationId, correlatableMessage, _entryOptions());
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
