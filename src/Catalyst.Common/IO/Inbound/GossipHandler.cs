using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Inbound
{
    public class GossipHandler : ObservableHandlerBase<AnySigned>
    {
        protected override void ChannelRead0(IChannelHandlerContext ctx, AnySigned msg)
        {
            ctx.FireChannelRead(ctx);
        }
    }
}
