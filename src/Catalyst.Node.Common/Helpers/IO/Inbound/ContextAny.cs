using DotNetty.Transport.Channels;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Node.Common.Helpers.IO.Inbound {
    public class ContextAny
    {
        public Any Message { get; }
        public IChannelHandlerContext Context { get; }

        public ContextAny(Any message, IChannelHandlerContext context)
        {
            Message = message;
            Context = context;
        }
    }
}