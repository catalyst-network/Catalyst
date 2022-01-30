#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf.WellKnownTypes;
using Serilog;
using MultiFormats;
using Catalyst.Modules.Network.Dotnetty.IO.Observers;
using Catalyst.Modules.Network.Dotnetty.Rpc.IO.Observers;
using DotNetty.Transport.Channels;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    /// <summary>
    ///     The GetPeerInfoRequestObserver
    /// </summary>
    public sealed class GetPeerInfoRequestObserver
        : RpcRequestObserverBase<GetPeerInfoRequest, GetPeerInfoResponse>,
            IRpcRequestObserver
    {
        private readonly IPeerRepository _peerRepository;

        public GetPeerInfoRequestObserver(IPeerSettings peerSettings,
            ILogger logger,
            IPeerRepository peerRepository)
            : base(logger, peerSettings)
        {
            _peerRepository = peerRepository;
        }

        /// <summary>
        ///     Handle the request for GetPeerInfo
        /// </summary>
        /// <param name="getPeerInfoRequest">The request</param>
        /// <param name="channelHandlerContext">The channel handler context</param>
        /// <param name="sender">The sender peer identifier</param>
        /// <param name="correlationId">The correlationId</param>
        /// <returns>The GetPeerInfoResponse</returns>
        protected override GetPeerInfoResponse HandleRequest(GetPeerInfoRequest getPeerInfoRequest,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress sender,
            ICorrelationId correlationId)
        {
            Guard.Argument(getPeerInfoRequest, nameof(getPeerInfoRequest)).NotNull();
            Guard.Argument(sender, nameof(sender)).NotNull();
            Logger.Debug("received message of type GetPeerInfoRequest");

            var peerInfo = _peerRepository.GetPeersByAddress(getPeerInfoRequest.Address)
               .Select(x =>
                    new PeerInfo
                    {
                        Address = x.Address.ToString(),
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
