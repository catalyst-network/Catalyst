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
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P.Messaging.Dto;
using Catalyst.Common.IO.Messaging;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Handlers
{
    public sealed class CorrelatableHandler : ChannelHandlerAdapter
    {
        private readonly IMessageCorrelationManager _messageCorrelationManager;

        public CorrelatableHandler(IMessageCorrelationManager messageCorrelationManager)
        {
            _messageCorrelationManager = messageCorrelationManager;
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            if (!CanCorrelate(message))
            {
                return context.Channel.CloseAsync();
            }
            
            IMessageDto messageDto = (IMessageDto) message;

            var protocolMessage = new ProtocolProtocolMessageFactory().GetMessage(
                messageDto,
                messageDto. .Payload.CorrelationId.ToGuid()
            );
            
            _messageCorrelationManager.AddPendingRequest(new CorrelatableMessage
            {
                Recipient = messageDto.Recipient,
                Content = messageDto.Message.ToProtocolMessage(),
                SentAt = DateTimeOffset.UtcNow
            });

            return context.Channel.WriteAndFlushAsync(protocolMessage);
        }
        
        private bool CanCorrelate(object msg)
        {
            return msg is IMessageDto;
        }
    }
}
