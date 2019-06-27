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
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Handlers
{
    public sealed class CorrelatableHandler : OutboundChannelHandlerBase<IOutboundDto>
    {
        private readonly IMessageCorrelationManager _messageCorrelationManager;

        public CorrelatableHandler(IMessageCorrelationManager messageCorrelationManager)
        {
            _messageCorrelationManager = messageCorrelationManager;
        }

        protected override Task WriteAsync0(IChannelHandlerContext context, IOutboundDto outbound)
        {
            if (outbound.MessageType.Name.Equals(MessageTypes.Request.Name))
            {
                _messageCorrelationManager.AddPendingRequest(new CorrelatableMessage
                {
                    Recipient = outbound.Recipient,
                    Content = outbound.Message.ToProtocolMessage(outbound.Sender.PeerId, Guid.NewGuid()),
                    SentAt = DateTimeOffset.UtcNow
                });
            }
            
            return context.WriteAsync(outbound);
        }
    }
}
