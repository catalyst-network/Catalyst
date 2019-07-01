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

using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Common.IO.Handlers
{
    public sealed class ProtoDatagramEncoderHandler : OutboundChannelHandlerBase<IMessageDto<ProtocolMessageSigned>>
    {
        protected override Task WriteAsync0(IChannelHandlerContext context, IMessageDto<ProtocolMessageSigned> message)
        {
            return context.WriteAndFlushAsync(message.Message.ToDatagram(message.Recipient.IpEndPoint));
        }

        public override void Flush(IChannelHandlerContext context)
        {
            context.Flush();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public ProtoDatagramEncoderHandler(ILogger logger) : base(logger) { }
    }
}
