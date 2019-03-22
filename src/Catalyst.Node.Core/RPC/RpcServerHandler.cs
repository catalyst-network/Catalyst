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
        //event to be fired when the RpcServerHandler recevives a GetNodeConfig command from the RpcClient
        public event EventHandler GetNodeConfig;

        //Delegate associated with the event
        protected virtual void onGetNodeConfig(EventArgs args)
        {
            EventHandler handler = GetNodeConfig;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public override void ChannelActive(IChannelHandlerContext contex)
        {
            contex.WriteAsync(string.Format("Welcome to {0} !\r\n", Dns.GetHostName()));
            contex.WriteAndFlushAsync(string.Format("It is {0} now !\r\n", DateTime.Now));
        }

        protected override void ChannelRead0(IChannelHandlerContext context, object message)
        {
            // Generate and write a response.
            string response, msg;
            bool close = false;
            msg = message as string;

            if (!string.IsNullOrEmpty(msg))
            {
                switch (msg)
                {
                    case "version":
                        response = NodeUtil.GetVersion();
                        break;
                    case "config":
                        //Fire the GetNodeConfig event
                        onGetNodeConfig(EventArgs.Empty);
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
            Console.WriteLine("{0}", e.StackTrace);
            contex.CloseAsync();
        }

        public override bool IsSharable => true;
    }
}