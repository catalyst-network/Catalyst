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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Common.IO.Handlers
{
    public sealed class CorrelatableHandler : OutboundChannelHandlerBase<IMessageDto<ProtocolMessage>>
    {
        private readonly IMessageCorrelationManager _messageCorrelationManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageCorrelationManager"></param>
        /// <param name="logger"></param>
        public CorrelatableHandler(IMessageCorrelationManager messageCorrelationManager, ILogger logger) : base(logger)
        {
            _messageCorrelationManager = messageCorrelationManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        protected override Task WriteAsync0(IChannelHandlerContext context, IMessageDto<ProtocolMessage> message)
        {
            if (message.MessageType.Name.Equals(MessageTypes.Request.Name))
            {
                _messageCorrelationManager.AddPendingRequest(new CorrelatableMessage
                {
                    Recipient = message.Recipient,
                    Content = message.Message.ToProtocolMessage(message.Sender.PeerId, Guid.NewGuid()),
                    SentAt = DateTimeOffset.UtcNow
                });
            }
            
            return context.WriteAsync(message);
        }
    }
}
