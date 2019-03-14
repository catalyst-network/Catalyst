namespace Catalyst.Node.Core.RPC
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using DotNetty.Transport.Channels;

    public class CLIRPCServerHandler : SimpleChannelInboundHandler<object>
    {
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

            if (string.IsNullOrEmpty(msg))
            {
                response = "Please type something.\r\n";
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

        public override void ChannelReadComplete(IChannelHandlerContext contex)
        {
            contex.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine("{0}", e.StackTrace);
            contex.CloseAsync();
        }

        public override bool IsSharable => true;
    }
}