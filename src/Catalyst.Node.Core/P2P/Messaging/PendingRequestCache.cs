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

        public TRequest TryMatchResponse<TRequest, TResponse>(TResponse response, IPeerIdentifier responderId) 
            where TRequest : class, IMessage<TRequest>
            where TResponse : class, IMessage<TResponse>
        {
            Guard.Argument(response, nameof(response)).NotNull()
               .Require(r => r.Descriptor.Name.EndsWith("Response"),
                    r => $"{nameof(response)} is of type {r.Descriptor.Name} which is not a known response type.");
            Guard.Argument(responderId, nameof(responderId)).NotNull();

            var found = RequestStore.TryFind(
                r => MatchResponseToRequest<TRequest, TResponse>(response, r, responderId),
                p => p,
                out PendingRequest matched);

            if(found) _ratingChangeSubject.OnNext(new PeerReputationChange(responderId, 10));

            return found ? matched.Content.FromAnySigned<TRequest>() : null;
        }

        private static bool MatchResponseToRequest<TRequest, TResponse>(TResponse response, PendingRequest request, IPeerIdentifier responderId)
            where TRequest : class, IMessage<TRequest> 
            where TResponse : class, IMessage<TResponse>
        {
            var isMatching = request.SentTo.Equals(responderId)
                 && request.Content.TypeUrl == response.Descriptor.ShortenedFullName()
                        //todo: clean this magic ?
                       .Replace("Response", "Request")
                 //todo: build a mechanism to append correlation Id on all proto Responses 
                 && request.Content.FromAnySigned<PingRequest>().CorrelationId == (response as PingResponse).CorrelationId;

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
