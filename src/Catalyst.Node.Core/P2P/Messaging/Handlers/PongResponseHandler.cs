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
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Google.Protobuf.WellKnownTypes;
using Serilog;
using PingResponse = Catalyst.Protocol.IPPN.PeerProtocol.Types.PingResponse;
using Catalyst.Node.Common.Helpers.IO;

namespace Catalyst.Node.Core.P2P.Messaging.Handlers
{
    public class PongResponseHandler : MessageHandlerBase<PingResponse>
    {
        public PongResponseHandler(IObservable<IChanneledMessage<Any>> messageStream, ILogger logger)
        : base(messageStream, logger) { }

        public override void HandleMessage(IChanneledMessage<Any> message)
        {
            Logger.Debug("received pong response");
            var deserialised = message.Payload.FromAny<PingResponse>();
            Logger.Debug("pong content is {0}", deserialised.CorrelationId);
        }
    }
}
