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
using System.IO;
using System.Reflection;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Gossip;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Dawn;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;

namespace Catalyst.Common.IO.Messaging
{
    public sealed class ProtoDatagramChannelHandler : ObservableHandlerBase<DatagramPacket>
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IGossipManager _gossipManager;

        public ProtoDatagramChannelHandler(IGossipManager gossipManager) { _gossipManager = gossipManager; }

        protected override void ChannelRead0(IChannelHandlerContext context, DatagramPacket packet)
        {
            Guard.Argument(context).NotNull();
            Guard.Argument(packet.Content.ReadableBytes).NotZero().NotNegative();

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(packet.Content.Array, 0, packet.Content.ReadableBytes);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var message = AnySigned.Parser.ParseFrom(memoryStream);
                var contextAny = new ChanneledAnySigned(context, message);

                if (!message.CheckIfMessageIsGossip())
                {
                    MessageSubject.OnNext(contextAny);
                } 

                context.FireChannelRead(message);
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception e)
        {
            Logger.Error(e, "Error in ProtoDatagramChannelHandler");
            context.CloseAsync().ContinueWith(_ => MessageSubject.OnCompleted());
        }
    }
}
