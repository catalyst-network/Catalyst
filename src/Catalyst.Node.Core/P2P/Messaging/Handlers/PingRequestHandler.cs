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

using System;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Serilog;
using Catalyst.Protocol.IPPN;

namespace Catalyst.Node.Core.P2P.Messaging.Handlers
{
    public sealed class PingRequestHandler 
        : MessageHandlerBase<PingRequest>,
            IP2PMessageHandler
    {
        private readonly IPeerIdentifier _peerIdentifier;

        public PingRequestHandler(IPeerIdentifier peerIdentifier,
            ILogger logger)
            : base(logger)
        {
            _peerIdentifier = peerIdentifier;
        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Logger.Information("Ping Message Received");
            var deserialised = message.Payload.FromAnySigned<PingRequest>();
            Logger.Debug("message content is {0}", deserialised);
            
            var datagramEnvelope = new P2PMessageFactory<PingResponse, P2PMessages>().GetMessageInDatagramEnvelope(
                new P2PMessageDto<PingResponse, P2PMessages>(
                    type: P2PMessages.PingRequest,
                    message: new PingResponse(),
                    recipient: new PeerIdentifier(message.Payload.PeerId),
                    sender: _peerIdentifier
                )
            );

            message.Context.Channel.WriteAndFlushAsync(datagramEnvelope);
        }
    }
}
