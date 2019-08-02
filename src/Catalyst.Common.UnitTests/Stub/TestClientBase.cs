using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.IO.Transport;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;

namespace Catalyst.Common.UnitTests.Stub
{
    public class TestClientBase : ClientBase
    {
        public TestClientBase(IChannelFactory channelFactory,
            ILogger logger,
            IEventLoopGroupFactory handlerEventEventLoopGroupFactory) : base(channelFactory, logger,
            handlerEventEventLoopGroupFactory)
        {
            Channel = Substitute.For<IChannel>();
        }
    }
}
