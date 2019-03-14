namespace Catalyst.Cli
{
    using System;
    using DotNetty.Transport.Channels;

    public class RPClientHandler : SimpleChannelInboundHandler<string>
    {
        protected override void ChannelRead0(IChannelHandlerContext contex, string msg)
        {
            Console.WriteLine(msg);
        }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine(DateTime.Now.Millisecond);
            Console.WriteLine("{0}", e.StackTrace);
            contex.CloseAsync();
        }
    }
}