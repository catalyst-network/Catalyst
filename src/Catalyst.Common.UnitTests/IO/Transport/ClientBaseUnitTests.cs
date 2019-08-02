using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.UnitTests.Stub;
using Catalyst.Protocol.Common;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Transport
{
    public sealed class ClientBaseUnitTests
    {
        [Fact]
        public void SendMessage_Should_Write_Message_To_Channel()
        {
            var messageDto = Substitute.For<IMessageDto<ProtocolMessage>>();
            var channelFactory = Substitute.For<ITcpClientChannelFactory>();
            var logger = Substitute.For<ILogger>();
            var eventLoopGroupFactory = Substitute.For<IEventLoopGroupFactory>();

            var testClientBase = new TestClientBase(channelFactory, logger, eventLoopGroupFactory);
            testClientBase.SendMessage(messageDto);

            testClientBase.Channel.Received(1).WriteAsync(messageDto);
        }
    }
}
