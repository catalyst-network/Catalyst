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
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Ipfs;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    public sealed class GetDeltaRequestObserver
        : RequestObserverBase<GetDeltaRequest, GetDeltaResponse>, IRpcRequestObserver
    {
        private readonly IHashProvider _hashProvider;
        private readonly IDeltaCache _deltaCache;

        public GetDeltaRequestObserver(IHashProvider hashProvider,
            IDeltaCache deltaCache,
            IPeerSettings peerSettings,
            ILogger logger) : base(logger, peerSettings)
        {
            _hashProvider = hashProvider;
            _deltaCache = deltaCache;
        }

        protected override GetDeltaResponse HandleRequest(GetDeltaRequest getDeltaRequest,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(getDeltaRequest, nameof(getDeltaRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();
            Logger.Verbose("received message of type GetDeltaRequest:");
            Logger.Verbose("{getDeltaRequest}", getDeltaRequest);

            var hash = _hashProvider.Cast(getDeltaRequest.DeltaDfsHash.ToByteArray());
            //var hashAsBase32 = getDeltaRequest.DeltaDfsHash.ToByteArray().ToBase32();

            _deltaCache.TryGetOrAddConfirmedDelta(hash, out var delta);

            return new GetDeltaResponse {Delta = delta};
        }
    }
}
