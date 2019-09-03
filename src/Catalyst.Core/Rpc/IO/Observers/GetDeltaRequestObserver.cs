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

using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Rpc.IO.Observers
{
    public sealed class GetDeltaRequestObserver
        : RequestObserverBase<GetDeltaRequest, GetDeltaResponse>, IRpcRequestObserver
    {
        private readonly IDeltaCache _deltaCache;

        public GetDeltaRequestObserver(IDeltaCache deltaCache,
            IPeerIdentifier peerIdentifier,
            ILogger logger) : base(logger, peerIdentifier)
        {
            _deltaCache = deltaCache;
        }

        protected override GetDeltaResponse HandleRequest(GetDeltaRequest getDeltaRequest,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(getDeltaRequest, nameof(getDeltaRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Verbose("received message of type GetDeltaRequest:");
            Logger.Verbose("{getDeltaRequest}", getDeltaRequest);

            var multiHash = getDeltaRequest.DeltaDfsHash.AsBase32Address();

            _deltaCache.TryGetConfirmedDelta(multiHash, out var delta);
            return new GetDeltaResponse {Delta = delta};
        }
    }
}
