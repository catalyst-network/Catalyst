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
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace Catalyst.Core.Rpc.IO.Observers
{
    /// <summary>
    /// The GetPeerInfoRequestObserver 
    /// </summary>
    public sealed class GetPeerInfoRequestObserver
        : RequestObserverBase<GetPeerInfoRequest, GetPeerInfoResponse>,
            IRpcRequestObserver
    {
        private readonly IPeerRepository _peerRepository;

        public GetPeerInfoRequestObserver(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IPeerRepository peerRepository)
            : base(logger, peerIdentifier)
        {
            _peerRepository = peerRepository;
        }

        /// <summary>
        /// Handle the request for GetPeerInfo
        /// </summary>
        /// <param name="getPeerInfoRequest">The request</param>
        /// <param name="channelHandlerContext">The channel handler context</param>
        /// <param name="senderPeerIdentifier">The sender peer identifier</param>
        /// <param name="correlationId">The correlationId</param>
        /// <returns>The GetPeerInfoResponse</returns>
        protected override GetPeerInfoResponse HandleRequest(GetPeerInfoRequest getPeerInfoRequest,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(getPeerInfoRequest, nameof(getPeerInfoRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug("received message of type GetPeerInfoRequest");

            var ip = getPeerInfoRequest.Ip;

            var peerInfo = _peerRepository.FindAll(m => m.PeerIdentifier.PeerId.Ip == ip
                 && m.PeerIdentifier.PeerId.PublicKey == getPeerInfoRequest.PublicKey)
               .Select(x =>
                    new PeerInfo
                    {
                        PeerId = x.PeerIdentifier.PeerId,
                        Reputation = x.Reputation,
                        BlackListed = x.BlackListed,
                        IsAwolPeer = x.IsAwolPeer,
                        InactiveFor = x.InactiveFor.ToDuration(),
                        LastSeen = x.LastSeen.ToTimestamp(),
                        Modified = x.Modified.HasValue ? x.Modified.Value.ToTimestamp() : null,
                        Created = x.Created.ToTimestamp()
                    }).ToList();

            var response = new GetPeerInfoResponse();
            response.PeerInfo.AddRange(peerInfo);
            return response;
        }
    }
}


