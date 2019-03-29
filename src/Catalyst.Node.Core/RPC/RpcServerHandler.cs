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

using Catalyst.Node.Common.Helpers.Util;

using System;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Node.Core.RPC
{
    public class RpcServerHandler : SimpleChannelInboundHandler<object>
    {
        public override void ChannelActive(IChannelHandlerContext contex)
        {
            contex.WriteAsync($"Welcome to {Dns.GetHostName()} !\r\n");
            contex.WriteAndFlushAsync($"It is {DateTime.Now} now !\r\n");
        }

        protected override void ChannelRead0(IChannelHandlerContext context, object message)
        {
            // Generate and write a response.
            string response;
            var close = false;
            var msg = message as string;

            if (!string.IsNullOrEmpty(msg))
            {
                switch (msg)
                {
                    case "version":
                        response = NodeUtil.GetVersion();
                        break;
                    case "config":
                        response = "";
                        break;
                    default:
                        response = "Invalid command.";
                        break;
                }
            }
            else if (string.Equals("bye", msg, StringComparison.OrdinalIgnoreCase))
            {
                response = "Have a good day!\r\n";
                close = true;
            }
            else
            {
                response = "Did you say '" + msg + "'?\r\n";
            }

            Task wait_close = context.WriteAndFlushAsync(response);
            if (close)
            {
                Task.WaitAll(wait_close);
                context.CloseAsync();
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext contex) { contex.Flush(); }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            contex.CloseAsync();
        }

        public override bool IsSharable => true;
    }
}