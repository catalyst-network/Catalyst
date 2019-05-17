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
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using ILogger = Serilog.ILogger;
using Catalyst.Common.P2P;
using Catalyst.Common.Rpc;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public sealed class GetVersionRequestHandler
        : CorrelatableMessageHandlerBase<VersionRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly IRpcMessageFactory _rpcMessageFactory;

        public GetVersionRequestHandler(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IRpcCorrelationCache messageCorrelationCache,
            IRpcMessageFactory rpcMessageFactory)
            : base(messageCorrelationCache, logger)
        {
            _rpcMessageFactory = rpcMessageFactory;
            _peerIdentifier = peerIdentifier;
        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Guard.Argument(message).NotNull("Received message cannot be null");
            
            Logger.Debug("received message of type VersionRequest");
            
            try
            {
                var response = _rpcMessageFactory.GetMessage(new MessageDto(
                        new VersionResponse
                        {
                            Version = NodeUtil.GetVersion()
                        },
                        MessageTypes.Tell,
                        new PeerIdentifier(message.Payload.PeerId),
                        _peerIdentifier),
                    message.Payload.CorrelationId.ToGuid());
                
                message.Context.Channel.WriteAndFlushAsync(response).GetAwaiter().GetResult();
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
