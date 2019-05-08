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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Common.Config;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public sealed class P2PCorrelationCache
        : MessageCorrelationCacheBase,
            IReputableCache
    {
        private readonly ReplaySubject<IPeerReputationChange> _ratingChangeSubject;
        private readonly BehaviorSubject<KeyValuePair<IPeerIdentifier, ByteString>> _peerEvictionSubject;
        
        public IObservable<IPeerReputationChange> PeerRatingChanges => _ratingChangeSubject.AsObservable();
        public IObservable<KeyValuePair<IPeerIdentifier, ByteString>> PeerEviction => _peerEvictionSubject.AsObservable();
        
        public P2PCorrelationCache(IMemoryCache cache,
            ILogger logger,
            TimeSpan cacheTtl = default) 
            : base(cache, logger, cacheTtl)
        {
            logger.Debug("P2PCorrelationCache resolved once");
            _ratingChangeSubject = new ReplaySubject<IPeerReputationChange>(0);
            _peerEvictionSubject = new BehaviorSubject<KeyValuePair<IPeerIdentifier, ByteString>>(NullObjects.EvictedMessage);
        }

        /// <summary>
        ///     Allows base constructor to get our none static PostEvictionDelegate callback method in a static context
        ///     by passing our delegated action.
        /// </summary>
        /// <returns></returns>
        protected override PostEvictionDelegate GetInheritorDelegate() { return ChangeReputationOnEviction; }

        public void ChangeReputationOnEviction(object key, object value, EvictionReason reason, object state)
        {
            //TODO: find a way to trigger the actual remove with correct reason
            //when the cache is not under pressure, eviction happens by token expiry :(
            var pendingRequest = (PendingRequest) value;
            _ratingChangeSubject.OnNext(new PeerReputationChange(pendingRequest.Recipient, -Constants.BaseReputationChange));
            _peerEvictionSubject.OnNext(new KeyValuePair<IPeerIdentifier, ByteString>(pendingRequest.Recipient, pendingRequest.Content.CorrelationId));
        }

        public override TRequest TryMatchResponse<TRequest, TResponse>(AnySigned response)
        {
            var matched = base.TryMatchResponse<TRequest, TResponse>(response);

            if (matched == null)
            {
                _ratingChangeSubject.OnNext(new PeerReputationChange(new PeerIdentifier(response.PeerId),
                    -Constants.BaseReputationChange * 10));
                return null;
            }

            _ratingChangeSubject.OnNext(new PeerReputationChange(new PeerIdentifier(response.PeerId),
                Constants.BaseReputationChange * 2));
            PendingRequests.Remove(response.CorrelationId);

            return matched;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            
            _ratingChangeSubject?.Dispose();
            base.Dispose(true);
        }
    }
}
