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

using System.Linq;
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
    ///     Handles the PeerListRequest message
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class PeerListRequestObserver
        : RequestObserverBase<GetPeerListRequest, GetPeerListResponse>,
            IRpcRequestObserver
    {
        /// <summary>
        ///     repository interface to storage
        /// </summary>
        private readonly IPeerRepository _peerRepository;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerListRequestObserver" /> class.
        /// </summary>
        /// <param name="peerSettings"></param>
        /// <param name="logger">The logger.</param>
        /// <param name="peerRepository"></param>
        public PeerListRequestObserver(IPeerSettings peerSettings,
            ILibP2PPeerClient peerClient,
            ILogger logger,
            IPeerRepository peerRepository)
            : base(logger, peerSettings, peerClient)
        {
            _peerRepository = peerRepository;
        }

        /// <summary>
        /// </summary>
        /// <param name="getPeerListRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override GetPeerListResponse HandleRequest(GetPeerListRequest getPeerListRequest,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(getPeerListRequest, nameof(getPeerListRequest)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();
            Logger.Debug("received message of type PeerListRequest");

            var peers = _peerRepository.GetAll().Select(x => x.Address.ToString());

            var response = new GetPeerListResponse();
            response.Peers.AddRange(peers);

            return response;
        }
    }
}
