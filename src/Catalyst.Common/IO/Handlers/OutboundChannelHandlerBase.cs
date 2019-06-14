using System.Threading.Tasks;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Handlers
{
    public abstract class OutboundChannelHandlerBase<I> : ChannelHandlerAdapter
    {
        public override Task WriteAsync(IChannelHandlerContext ctx, object msg)
        {
            if (msg is I msg1)
            {
                return WriteAsync0(ctx, msg1);
            }

            return ctx.WriteAsync(msg);
        }

        protected abstract Task WriteAsync0(IChannelHandlerContext ctx, I msg);
    }
}
