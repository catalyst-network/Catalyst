using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.IO.Transport;
using Catalyst.Common.UnitTests.Stub;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Transport
{
    public sealed class TcpServerUnitTests
    {
        [Fact]
        public void TcpServer_Should_Dispose()
        {
            var tcpServerChannelFactory = Substitute.For<ITcpServerChannelFactory>();
            var logger = Substitute.For<ILogger>();
            var eventLoopGroupFactory = Substitute.For<IEventLoopGroupFactory>();
            var tcpServer = new TestTcpServer(tcpServerChannelFactory, logger, eventLoopGroupFactory);
            tcpServer.DisposeProxy(true);

            logger.Received(1).Debug($"Disposing{typeof(TestTcpServer).Name}");
        }

        [Fact]
        public void TcpServer_Should_Not_Dispose()
        {
            var tcpServerChannelFactory = Substitute.For<ITcpServerChannelFactory>();
            var logger = Substitute.For<ILogger>();
            var eventLoopGroupFactory = Substitute.For<IEventLoopGroupFactory>();
            var tcpServer = new TestTcpServer(tcpServerChannelFactory, logger, eventLoopGroupFactory);
            tcpServer.DisposeProxy(false);

            logger.DidNotReceive().Debug($"Disposing{typeof(TestTcpServer).Name}");
        }
    }
}
