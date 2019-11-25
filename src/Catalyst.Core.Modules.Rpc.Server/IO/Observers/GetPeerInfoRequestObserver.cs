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
using Catalyst.Core.Lib.P2P.Service;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf.WellKnownTypes;
using Serilog;
using IPeerService = Catalyst.Core.Lib.P2P.Service.IPeerService;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    /// <summary>
    /// The GetPeerInfoRequestObserver 
    /// </summary>
    public sealed class GetPeerInfoRequestObserver
        : RequestObserverBase<GetPeerInfoRequest, GetPeerInfoResponse>,
            IRpcRequestObserver
    {
        private readonly IPeerService _peerService;

        public GetPeerInfoRequestObserver(IPeerSettings peerSettings,
            ILogger logger,
            IPeerService peerService)
            : base(logger, peerSettings)
        {
            _peerService = peerService;
        }

        /// <summary>
        /// Handle the request for GetPeerInfo
        /// </summary>
        /// <param name="getPeerInfoRequest">The request</param>
        /// <param name="channelHandlerContext">The channel handler context</param>
        /// <param name="senderPeerId">The sender peer identifier</param>
        /// <param name="correlationId">The correlationId</param>
        /// <returns>The GetPeerInfoResponse</returns>
        protected override GetPeerInfoResponse HandleRequest(GetPeerInfoRequest getPeerInfoRequest,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(getPeerInfoRequest, nameof(getPeerInfoRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();
            Logger.Debug("received message of type GetPeerInfoRequest");

            var ip = getPeerInfoRequest.Ip;

            var peerInfo = _peerService.GetPeersByIpAndPublicKey(ip, getPeerInfoRequest.PublicKey)
               .Select(x =>
                    new PeerInfo
                    {
                        PeerId = x.PeerId,
                        Reputation = x.Reputation,
                        IsBlacklisted = x.BlackListed,
                        IsUnreachable = x.IsAwolPeer,
                        InactiveFor = x.InactiveFor.ToDuration(),
                        LastSeen = x.LastSeen.ToTimestamp(),
                        Modified = x.Modified?.ToTimestamp(),
                        Created = x.Created.ToTimestamp()
                    }).ToList();

            var response = new GetPeerInfoResponse();
            response.PeerInfo.AddRange(peerInfo);
            return response;
        }
    }
}


