using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Common.IO.EventLoop;
using Catalyst.Common.IO.Transport.Channels;
using Catalyst.Common.UnitTests.Stub;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Transport.Channels
{
    public sealed class TcpServerChannelFactoryTests
    {
        public TcpServerChannelFactoryTests()
        {
            var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
            {
                TcpServerHandlerWorkerThreads = 2
            };

            _eventLoopGroupFactory = new TcpServerEventLoopGroupFactory(eventLoopGroupFactoryConfiguration);
            _ipAddress = IPAddress.Loopback;
            _port = 9000;

            _testTcServerChannelFactory = new TestTcpServerChannelFactory();
        }

        private readonly TestTcpServerChannelFactory _testTcServerChannelFactory;
        private readonly TcpServerEventLoopGroupFactory _eventLoopGroupFactory;
        private readonly IPAddress _ipAddress;
        private readonly int _port;

        [Fact]
        public async Task Bootstrap_Should_Return_Channel()
        {
            var certificate = Substitute.For<X509Certificate2>();
            var channel =
                await _testTcServerChannelFactory.Bootstrap(_eventLoopGroupFactory, _ipAddress, _port, certificate);

            channel.Should().BeAssignableTo<IChannel>();
        }

        [Fact]
        public async Task BuildChannel_Should_Return_IObservableChannel()
        {
            var observableChannel =
                await _testTcServerChannelFactory.BuildChannel(_eventLoopGroupFactory, _ipAddress, _port);

            observableChannel.Should().BeOfType<ObservableChannel>();
        }
    }
}
