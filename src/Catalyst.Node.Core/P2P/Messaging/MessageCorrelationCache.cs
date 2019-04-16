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
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.Interfaces.P2P.Messaging;
using Catalyst.Node.Common.P2P;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public sealed class MessageCorrelationCache : IMessageCorrelationCache
    {
        public static readonly int BaseReputationChange = 1;
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(10);
        private readonly IMemoryCache _pendingRequests;
        private readonly ReplaySubject<IPeerReputationChange> _ratingChangeSubject;
        private readonly MemoryCacheEntryOptions _entryOptions;

        public IObservable<IPeerReputationChange> PeerRatingChanges => _ratingChangeSubject.AsObservable();
        public TimeSpan CacheTtl { get; }

        public MessageCorrelationCache(IMemoryCache cache, TimeSpan cacheTtl = default)
        {
            CacheTtl = cacheTtl == default ? DefaultTtl : cacheTtl;
            _pendingRequests = cache;
            _ratingChangeSubject = new ReplaySubject<IPeerReputationChange>(0);
            _entryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(CacheTtl).Token))
               .RegisterPostEvictionCallback(ChangeReputationOnEviction);
        }

        public void AddPendingRequest(PendingRequest pendingRequest)
        {
            _pendingRequests.Set(pendingRequest.Content.CorrelationId, pendingRequest, _entryOptions);
        }

        private void ChangeReputationOnEviction(object key, object value, EvictionReason reason, object state)
        {
            //TODO: find a way to trigger the actual remove with correct reason
            //when the cache is not under pressure, eviction happens by token expiry :(
            //if (reason == EvictionReason.Removed) {return;}
            var pendingRequest = (PendingRequest) value;
            _ratingChangeSubject.OnNext(new PeerReputationChange(pendingRequest.SentTo, -BaseReputationChange));
        }

        public TRequest TryMatchResponse<TRequest, TResponse>(AnySigned response)
            where TRequest : class, IMessage<TRequest>
            where TResponse : class, IMessage<TResponse>
        {
            Guard.Argument(response, nameof(response)).NotNull()
               .Require(r => typeof(TResponse).ShortenedProtoFullName().Equals(response.TypeUrl))
               .Require(r => typeof(TRequest).ShortenedProtoFullName().Equals(r.TypeUrl.GetRequestType()));

            var found = _pendingRequests.TryGetValue(response.CorrelationId, out PendingRequest matched);

            if (!found)
            {
                return null;
            }

            _ratingChangeSubject.OnNext(new PeerReputationChange(new PeerIdentifier(response.PeerId), BaseReputationChange * 2));
            _pendingRequests.Remove(response.CorrelationId);

            return matched.Content.FromAnySigned<TRequest>();
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _pendingRequests?.Dispose();
            _ratingChangeSubject.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
