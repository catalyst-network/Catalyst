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
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Node.Common.Interfaces.P2P.Messaging;
using Catalyst.Node.Common.P2P;
using Catalyst.Protocol.IPPN;
using Dawn;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public class PendingRequestCache : IPendingRequestCache
    {
        public PendingRequestCache(IRepository<PendingRequest> responseStore)
        {
            ResponseStore = responseStore;
        }

        public IRepository<PendingRequest> ResponseStore { get; }

        public PendingRequest TryMatchResponseAsync(PingResponse response, IPeerIdentifier responderId)
        {
            Guard.Argument(response, nameof(response)).NotNull()
               .Require(r => r.CorrelationId != null);
            Guard.Argument(responderId, nameof(responderId)).NotNull();

            return !ResponseStore.TryFind(r => r.TargetNodeId.Equals(responderId), 
                p => p, 
                out PendingRequest matched) ? matched : null;
        }

        public IObservable<IPeerReputationChange> PeerRatingChanges { get; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) {return;}

            ResponseStore?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
