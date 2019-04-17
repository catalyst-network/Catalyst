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

using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging.Handlers
{
    public class GetNeighbourRequestHandler : ReputableCorrelatorMessageHandler<PeerNeighborsRequest, IReputableCache>, IP2PMessageHandler
    {
        private readonly IPeerIdentifier _peerIdentifier;

        public GetNeighbourRequestHandler(IPeerIdentifier peerIdentifier,
            IReputableCache reputableCache,
            ILogger logger)
            : base(reputableCache, logger)
        {
            _peerIdentifier = peerIdentifier;
        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Logger.Debug("PeerNeighborsRequest Message Received");

            var datagramEnvelope = new P2PMessageFactory<PeerNeighborsResponse, P2PMessages>().GetMessageInDatagramEnvelope(
                new P2PMessageDto<PeerNeighborsResponse, P2PMessages>(
                    type: P2PMessages.PingRequest,
                    message: new PeerNeighborsResponse(),
                    
                    // {
                    //     PeerIds = { }
                    // },
                    destination: new PeerIdentifier(message.Payload.PeerId).IpEndPoint,
                    peerIdentifier: _peerIdentifier
                )
            );

            message.Context.Channel.WriteAndFlushAsync(datagramEnvelope);
        }
    }
}
