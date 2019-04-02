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
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.Interfaces.P2P.Messaging;
using Catalyst.Node.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Dawn;
using Google.Protobuf;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public class PendingRequestCache : IPendingRequestCache, IDisposable
    {
        public IObservable<IPeerReputationChange> PeerRatingChanges => _ratingChangeSubject.AsObservable();

        private readonly ReplaySubject<IPeerReputationChange> _ratingChangeSubject;

        public PendingRequestCache(IRepository<PendingRequest> requestStore)
        {
            RequestStore = requestStore;
            _ratingChangeSubject = new ReplaySubject<IPeerReputationChange>(0);
        }

        public IRepository<PendingRequest> RequestStore { get; }

        public TRequest TryMatchResponse<TRequest, TResponse>(AnySigned response) 
            where TRequest : class, IMessage<TRequest>
            where TResponse : class, IMessage<TResponse>
        {
            Guard.Argument(response, nameof(response)).NotNull()
               .Require(r => typeof(TResponse).ShortenedProtoFullName().Equals(response.TypeUrl))
               .Require(r => typeof(TRequest).ShortenedProtoFullName().Equals(r.TypeUrl.GetRequestType()));

            var found = RequestStore.TryFind(
                r => MatchResponseToRequest<TRequest, TResponse>(r, response),
                p => p,
                out PendingRequest matched);

            if (!found) { return null; }

            _ratingChangeSubject.OnNext(new PeerReputationChange(new PeerIdentifier(response.PeerId), 10));
            
            return matched.Content.FromAnySigned<TRequest>();
        }

        private static bool MatchResponseToRequest<TRequest, TResponse>( 
            PendingRequest request, AnySigned response)
            where TRequest : class, IMessage<TRequest> 
            where TResponse : class, IMessage<TResponse>
        {
            var isMatching = request.SentTo.PeerId.Equals(response.PeerId)
                 && request.Content.TypeUrl == response.TypeUrl.GetRequestType()
                 && request.Content.CorrelationId == response.CorrelationId;

            return isMatching;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) {return;}

            RequestStore?.Dispose();
            _ratingChangeSubject.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
