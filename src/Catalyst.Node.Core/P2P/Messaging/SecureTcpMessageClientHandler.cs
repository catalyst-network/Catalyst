using System;
using System.Reflection;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging {
    public class SecureTcpMessageClientHandler : SimpleChannelInboundHandler<string>
    { 
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);
        
        public override void ChannelActive(IChannelHandlerContext context)
        {
            Console.WriteLine("123");
            Logger.Information(context.Channel.ToString());
        }
        
        protected override void ChannelRead0(IChannelHandlerContext context, string msg) 
            => Logger.Information(msg);

        public override void ExceptionCaught(IChannelHandlerContext context, Exception e)
        {
            Logger.Error(e, "Error in P2P client");
            context.CloseAsync();
        }
    }
}