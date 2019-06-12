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

using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Handlers
{
    public sealed class CorrelationHandler : SimpleChannelInboundHandler<ProtocolMessage>
    {
        private readonly IMessageCorrelationManager _messageCorrelationManager;

        public CorrelationHandler(IMessageCorrelationManager messageCorrelationManager)
        {
            _messageCorrelationManager = messageCorrelationManager;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, ProtocolMessage message)
        {
            if (message.CheckIfMessageIsBroadcast())
            {
                ctx.FireChannelRead(message);
            }
            
            //@TODO should not negate try match response but currently not storing in cache when sent
            else if (!_messageCorrelationManager.TryMatchResponse(message))
            {
                ctx.FireChannelRead(message);                
            }
            else
            {
                // @TODO maybe we want to send a message why we close the channel, if we do we will need a Correlation handler for Tcp && Udp to write back correctly.
                // ctx.Channel.WriteAndFlushAsync(datagramEnvelope);
                ctx.CloseAsync();
            }
        }
    }
}
