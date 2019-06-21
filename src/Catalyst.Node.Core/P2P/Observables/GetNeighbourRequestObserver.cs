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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Protocol.IPPN;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P.Observables
{
    public sealed class GetNeighbourRequestObserver
        : RequestObserverBase<PeerNeighborsRequest, PeerNeighborsResponse>,
            IP2PMessageObserver
    {
        private readonly IRepository<Peer> _repository;

        public GetNeighbourRequestObserver(IPeerIdentifier peerIdentifier,
            IRepository<Peer> repository,
            ILogger logger)
            : base(logger, peerIdentifier)
        { 
            _repository = repository;
        }

        public override IMessage<PeerNeighborsResponse> HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("PeerNeighborsRequest Message Received");

            // @TODO can't mock FindAll return properly so just do GetAll and filter with linq for now
            // var activePeersList = _repository.FindAll(new Specification<Peer>(p => p.IsAwolPeer == false));
            var activePeersList = _repository.GetAll().Where(p => p.IsAwolPeer == false).ToList();
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
