using System;
using DotNetty.Transport.Channels;

namespace Catalyst.Node.Core.P2P.Messaging {
    public class SecureTcpMessageClientHandler : SimpleChannelInboundHandler<string>
    {
        protected override void ChannelRead0(IChannelHandlerContext context, string msg) => Console.WriteLine(msg);

        public override void ExceptionCaught(IChannelHandlerContext context, Exception e)
        {
            Console.WriteLine(DateTime.Now.Millisecond);
            Console.WriteLine(e.StackTrace);
            context.CloseAsync();
        }
    }
}