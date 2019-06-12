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

using System.IO;
using Catalyst.Protocol.Common;
using Dawn;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Catalyst.Common.IO.Inbound.Handlers
{
    public sealed class ProtoDatagramHandler : SimpleChannelInboundHandler<DatagramPacket>
    {
        protected override void ChannelRead0(IChannelHandlerContext context, DatagramPacket packet)
        {
            Guard.Argument(context).NotNull();
            Guard.Argument(packet.Content.ReadableBytes).NotZero().NotNegative();

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(packet.Content.Array, 0, packet.Content.ReadableBytes);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var signedMessage = ProtocolMessageSigned.Parser.ParseFrom(memoryStream);
                context.FireChannelRead(signedMessage);
            }
        }
    }
}
