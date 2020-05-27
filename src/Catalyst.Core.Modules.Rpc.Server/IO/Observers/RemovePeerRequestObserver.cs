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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;
using MultiFormats;
using Lib.P2P.Protocols;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    /// <summary>
    ///     Remove Peer handler
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class RemovePeerRequestObserver
        : RequestObserverBase<RemovePeerRequest, RemovePeerResponse>,
            IRpcRequestObserver
    {
        /// <summary>The peer discovery</summary>
        private readonly IPeerRepository _peerRepository;

        /// <summary>Initializes a new instance of the <see cref="RemovePeerRequestObserver" /> class.</summary>
        /// <param name="peerSettings"></param>
        /// <param name="peerRepository">The peer discovery.</param>
        /// <param name="logger">The logger.</param>
        public RemovePeerRequestObserver(IPeerSettings peerSettings,
            IPeerRepository peerRepository,
            ILibP2PPeerClient peerClient,
            ILogger logger) : base(logger, peerSettings, peerClient)
        {
            _peerRepository = peerRepository;
        }

        /// <summary>
        /// </summary>
        /// <param name="removePeerRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override RemovePeerResponse HandleRequest(RemovePeerRequest removePeerRequest,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(removePeerRequest, nameof(removePeerRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();
            Logger.Debug("Received message of type RemovePeerRequest");

            var peerDeletedCount = _peerRepository.DeletePeersByAddress(removePeerRequest.PeerId);

            return new RemovePeerResponse
            {
                DeletedCount = peerDeletedCount
            };
        }
    }
}
