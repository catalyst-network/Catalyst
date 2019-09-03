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
using Catalyst.Core.Config;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Observers;
using Catalyst.Core.P2P.Models;
using Catalyst.Core.P2P.Repository;
using Catalyst.Protocol.IPPN;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;
using SharpRepository.Repository.Specifications;

namespace Catalyst.Core.P2P.IO.Observers
{
    public sealed class GetNeighbourRequestObserver
        : RequestObserverBase<PeerNeighborsRequest, PeerNeighborsResponse>,
            IP2PMessageObserver
    {
        private readonly IPeerRepository _repository;

        public GetNeighbourRequestObserver(IPeerIdentifier peerIdentifier,
            IPeerRepository repository,
            ILogger logger)
            : base(logger, peerIdentifier)
        { 
            _repository = repository;
        }

        /// <summary>
        ///     Processes a GetNeighbourResponse item from stream.
        /// </summary>
        /// <param name="peerNeighborsRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override PeerNeighborsResponse HandleRequest(PeerNeighborsRequest peerNeighborsRequest,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(peerNeighborsRequest, nameof(peerNeighborsRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            
            Logger.Debug("PeerNeighborsRequest Message Received");

            var activePeersList = _repository
               .FindAll(new Specification<Peer>(p => !p.IsAwolPeer))
               .Take(Constants.AngryPirate) // 😂
               .ToList();
            
            Guard.Argument(activePeersList).MinCount(1);

            var peerNeighborsResponseMessage = new PeerNeighborsResponse();
            
            for (var i = 0; i < Constants.NumberOfRandomPeers; i++)
            {
                peerNeighborsResponseMessage.Peers.Add(activePeersList.RandomElement().PeerIdentifier.PeerId);
            }

            return peerNeighborsResponseMessage;
        }
    }
}
