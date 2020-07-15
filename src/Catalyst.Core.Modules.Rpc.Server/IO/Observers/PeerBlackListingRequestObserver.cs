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
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog;
using MultiFormats;
using Catalyst.Modules.Network.Dotnetty.IO.Observers;
using Catalyst.Modules.Network.Dotnetty.Rpc.IO.Observers;
using DotNetty.Transport.Channels;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    public sealed class PeerBlackListingRequestObserver
        : RpcRequestObserverBase<SetPeerBlackListRequest, SetPeerBlackListResponse>,
            IRpcRequestObserver
    {
        /// <summary>
        ///     The PeerBlackListingRequestHandler
        /// </summary>
        private readonly IPeerRepository _peerRepository;

        public PeerBlackListingRequestObserver(IPeerSettings peerSettings,
            ILogger logger,
            IPeerRepository peerRepository)
            : base(logger, peerSettings)
        {
            _peerRepository = peerRepository;
        }

        /// <summary>
        /// </summary>
        /// <param name="setPeerBlackListRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="sender"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override SetPeerBlackListResponse HandleRequest(SetPeerBlackListRequest setPeerBlackListRequest,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress sender,
            ICorrelationId correlationId)
        {
            Guard.Argument(setPeerBlackListRequest, nameof(setPeerBlackListRequest)).NotNull();
            Guard.Argument(sender, nameof(sender)).NotNull();
            Logger.Information("received message of type PeerBlackListingRequest");

            var peerItem = _peerRepository.GetAll().FirstOrDefault(m => m.Address == setPeerBlackListRequest.Address);

            return peerItem == null
                ? ReturnResponse(false, string.Empty)
                : ReturnResponse(setPeerBlackListRequest.Blacklist, peerItem.Address.ToString());
        }

        /// <summary>
        ///     Returns the response.
        /// </summary>
        /// <param name="blacklist">if set to <c>true</c> [blacklist].</param>
        /// <param name="publicKey">The public key.</param>
        /// <param name="ip">The ip.</param>
        private SetPeerBlackListResponse ReturnResponse(bool blacklist, string address)
        {
            return new SetPeerBlackListResponse
            {
                Blacklist = blacklist,
                Address = address
            };
        }
    }
}
