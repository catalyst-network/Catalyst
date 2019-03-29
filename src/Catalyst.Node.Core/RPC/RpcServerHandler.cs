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
            Console.WriteLine(@"{0}", e.StackTrace);
            contex.CloseAsync();
        }

        public override bool IsSharable => true;
    }
}