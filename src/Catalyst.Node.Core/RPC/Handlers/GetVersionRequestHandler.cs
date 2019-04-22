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
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public class GetVersionRequestHandler
        : CorrelatableMessageHandlerBase<VersionRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        private readonly PeerId _peerId;

        public GetVersionRequestHandler(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IMessageCorrelationCache messageCorrelationCache)
            : base(messageCorrelationCache, logger)
        {
            _peerId = peerIdentifier.PeerId;
        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Guard.Argument(message).NotNull();
            
            Logger.Debug("received message of type VersionRequest");
            try
            {
                var deserialised = message.Payload.FromAnySigned<VersionRequest>();
                
                Guard.Argument(deserialised).NotNull();
                
                Logger.Debug("message content is {0}", deserialised);
                
                var response = new VersionResponse
                {
                    Version = NodeUtil.GetVersion()
                };

                var anySignedResponse = response.ToAnySigned(_peerId, message.Payload.CorrelationId.ToGuid());
                
                message.Context.Channel.WriteAndFlushAsync(anySignedResponse).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetVersionRequest after receiving message {0}", message);
                throw;
            }
        }
    }
}
