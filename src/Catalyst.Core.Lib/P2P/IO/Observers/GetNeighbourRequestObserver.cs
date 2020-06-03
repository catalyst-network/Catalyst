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
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Dawn;
using DotNetty.Transport.Channels;
using Lib.P2P.Protocols;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Lib.P2P.IO.Observers
{
    public sealed class GetNeighbourRequestObserver
        : RequestObserverBase<PeerNeighborsRequest, PeerNeighborsResponse>,
            IP2PMessageObserver
    {
        private readonly IPeerRepository _repository;

        public GetNeighbourRequestObserver(IPeerSettings peerSettings,
            IPeerRepository repository,
            ILibP2PPeerClient peerClient,
            ILogger logger)
            : base(logger, peerSettings, peerClient)
        {
            _repository = repository;
        }

        /// <summary>
        ///     Processes a GetNeighbourResponse item from stream.
        /// </summary>
        /// <param name="peerNeighborsRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override PeerNeighborsResponse HandleRequest(PeerNeighborsRequest peerNeighborsRequest,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(peerNeighborsRequest, nameof(peerNeighborsRequest)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();

            Logger.Debug("PeerNeighborsRequest Message Received");

            var activePeersList = _repository.GetActivePeers(Constants.NumberOfRandomPeers);

            Guard.Argument(activePeersList).MinCount(1);

            var peerNeighborsResponseMessage = new PeerNeighborsResponse();

            for (var i = 0; i < Constants.NumberOfRandomPeers; i++)
            {
                peerNeighborsResponseMessage.Peers.Add(activePeersList.RandomElement().Address.ToString());
            }

            return peerNeighborsResponseMessage;
        }
    }
}
