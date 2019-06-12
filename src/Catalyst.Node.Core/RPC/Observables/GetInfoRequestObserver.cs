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
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
{
    public sealed class GetInfoRequestObserver
        : ObserverBase<GetInfoRequest>,
            IRpcRequestObserver
    {
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly IRpcServerSettings _config;
        private readonly IMessageFactory _messageFactory;

        public GetInfoRequestObserver(IPeerIdentifier peerIdentifier,
            IRpcServerSettings config,
            IMessageFactory messageFactory,
            ILogger logger) : base(logger)
        {
            _messageFactory = messageFactory;
            _peerIdentifier = peerIdentifier;
            _config = config;
        }

        protected override void Handler(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Guard.Argument(messageDto).NotNull();
            
            Logger.Debug("received message of type GetInfoRequest");
            try
            {
                var deserialised = messageDto.Payload.FromProtocolMessage<GetInfoRequest>();
                
                Guard.Argument(deserialised).NotNull();
                
                Logger.Debug("message content is {0}", deserialised);

                var serializedList = JsonConvert.SerializeObject(
                    _config.NodeConfig.GetSection("CatalystNodeConfiguration").AsEnumerable(), 
                    Formatting.Indented);

                var response = _messageFactory.GetMessage(new MessageDto(
                        new GetInfoResponse
                        {
                            Query = serializedList
                        },
                        MessageTypes.Response,
                        new PeerIdentifier(messageDto.Payload.PeerId), 
                        _peerIdentifier
                    ),
                    messageDto.Payload.CorrelationId.ToGuid());
                
                messageDto.Context.Channel.WriteAndFlushAsync(response).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoRequest after receiving message {0}", messageDto);
                throw;
            }
        }
    }
}
