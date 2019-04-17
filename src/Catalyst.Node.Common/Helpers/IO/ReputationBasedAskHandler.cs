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
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.IO.Outbound;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Node.Common.Interfaces.P2P.Messaging;
using Catalyst.Node.Common.P2P;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO
{
    public abstract class ReputationBasedAskHandler<TProto, TReputableCache> : CorrelatableMessageHandler<TProto, TReputableCache>
        where TProto : IMessage
        where TReputableCache : IMessageCorrelationCache
    {
        private readonly TReputableCache _reputableCache;
        
        public ReputationBasedAskHandler(TReputableCache reputableCache, ILogger logger) : base(reputableCache, logger)
        {
            _reputableCache = reputableCache;
        }
        
        /// <summary>
        ///     Adds a new message to the correlation cache before we flush it away down the socket.
        /// </summary>
        /// <param name="message"></param>
        public override void HandleMessage(IChanneledMessage<AnySigned> message)
        {           
            _reputableCache.AddPendingRequest(new PendingRequest
            {
                Content = message.Payload,
                Recipient = new PeerIdentifier(message.Payload.PeerId),
                SentAt = DateTimeOffset.UtcNow
            });
            
            Handler(message);
        }
    }
}
