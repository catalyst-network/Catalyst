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
using Catalyst.Core.IO.Observers;
using Catalyst.Core.P2P.Repository;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Rpc.IO.Observers
{
    /// <summary>
    /// Peer count request handler
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class PeerCountRequestObserver
        : RequestObserverBase<GetPeerCountRequest, GetPeerCountResponse>,
            IRpcRequestObserver
    {
        /// <summary>The peer discovery</summary>
        private readonly IPeerRepository _peerRepository;

        /// <summary>Initializes a new instance of the <see cref="PeerCountRequestObserver"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peerRepository">The peer discovery.</param>
        /// <param name="logger">The logger.</param>
        public PeerCountRequestObserver(IPeerIdentifier peerIdentifier,
            IPeerRepository peerRepository,
            ILogger logger) :
            base(logger, peerIdentifier)
        {
            _peerRepository = peerRepository;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="getPeerCountRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override GetPeerCountResponse HandleRequest(GetPeerCountRequest getPeerCountRequest,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(getPeerCountRequest, nameof(getPeerCountRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            var peerCount = _peerRepository.GetAll().Count();

            return new GetPeerCountResponse
            {
                PeerCount = peerCount
            };
        }
    }
}
