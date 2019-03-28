/*
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

using System;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Core.P2P.Messaging.Handlers;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf.WellKnownTypes;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC
{
    public class GetInfoRequestHandler : MessageHandlerBase<GetInfoRequest>
    {
        private readonly IRpcServerSettings _config;

        public GetInfoRequestHandler(
            IObservable<IChanneledMessage<Any>> messageStream,
            IRpcServerSettings config,
            ILogger logger)
            : base(messageStream, logger)
        {
            _config = config;
        }

        public override void HandleMessage(IChanneledMessage<Any> message)
        {
            if(message == NullObjects.ChanneledAny) {return;}
            Logger.Debug("received message of type GetInfoRequest");
            try
            {
                var deserialised = message.Payload.FromAny<GetInfoRequest>();
                Logger.Debug("message content is {0}", deserialised);
                var response = new GetInfoResponse
                {
                    Query = "replying to you with a config"
                };

                message.Context.Channel.WriteAndFlushAsync(response.ToAny()).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, 
                    "Failed to handle GetInfoRequest after receiving message {0}", message);
                throw;
            }
        }
    }
}
