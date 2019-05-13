﻿#region LICENSE

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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace Catalyst.Common.IO.Messaging
{
    public class MessageCorrelationCacheBase
        : IMessageCorrelationCache
    {
        public TimeSpan CacheTtl { get; }
        
        protected readonly ILogger Logger;
        protected readonly IMemoryCache PendingRequests;
        private readonly MemoryCacheEntryOptions _entryOptions;

        protected MessageCorrelationCacheBase(IMemoryCache cache,
            ILogger logger,
            TimeSpan cacheTtl = default)
        {
            Logger = logger;
            CacheTtl = cacheTtl == default ? Constants.CorrelationTtl : cacheTtl;
            PendingRequests = cache;
            _entryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(CacheTtl).Token))
               .RegisterPostEvictionCallback(GetInheritorDelegate());
        }

        protected virtual PostEvictionDelegate GetInheritorDelegate()
        {
            Logger.Fatal("MessageCorrelationCache.GetInheritorDelegate() called without inheritor.");
            throw new NotImplementedException("Inheritors that uses the default constructor must implement the GetInheritorDelegate() method."); 
        }

        public void AddPendingRequest(PendingRequest pendingRequest)
        {
            PendingRequests.Set(pendingRequest.Content.CorrelationId, pendingRequest, _entryOptions);
        }

        public virtual TRequest TryMatchResponse<TRequest, TResponse>(AnySigned response)
            where TRequest : class, IMessage<TRequest>
            where TResponse : class, IMessage<TResponse>
        {
            Guard.Argument(response, nameof(response)).NotNull()
               .Require(r => typeof(TResponse).ShortenedProtoFullName().Equals(response.TypeUrl))
               .Require(r => typeof(TRequest).ShortenedProtoFullName().Equals(r.TypeUrl.GetRequestType()));

            var found = PendingRequests.TryGetValue(response.CorrelationId, out PendingRequest matched);

            return !found ? null : matched.Content.FromAnySigned<TRequest>();
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
