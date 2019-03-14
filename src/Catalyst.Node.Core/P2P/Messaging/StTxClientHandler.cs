using System;
using System.Reflection;
using Catalyst.Protocols.Transaction;
using DotNetty.Transport.Channels;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging {
    public class AnyTypeClientHandler : SimpleChannelInboundHandler<Any>
    { 
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        protected override void ChannelRead0(IChannelHandlerContext context, Any message)
        {
            var log = JsonConvert.SerializeObject(message);
            var innerLog = "unknown type";
            if(message.TypeUrl == typeof(StTx).FullName)
            {
                var tx = message.FromAny<StTx>();
                innerLog = JsonConvert.SerializeObject(tx);
            }
            else if (message.TypeUrl == typeof(Key).FullName)
            {
                var key = message.FromAny<Key>();
                innerLog = JsonConvert.SerializeObject(key);
            }
            Logger.Information(innerLog + Environment.NewLine);
        } 

        public override void ExceptionCaught(IChannelHandlerContext context, Exception e)
        {
            Logger.Error(e, "Error in P2P client");
            context.CloseAsync();
        }
    }
}