using DotNetty.Transport.Channels;

namespace Catalyst.Common.Interfaces.IO
{
    public interface IRpcBusinessEventFactory
    {
        IEventLoopGroup NewRpcClientLoopGroup();
    }
}
