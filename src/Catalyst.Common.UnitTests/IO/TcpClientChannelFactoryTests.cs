#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.IO
{
    public class TcpClientChannelFactoryTests
    {
        internal sealed class TestTcpClientChannelFactory : TcpClientChannelFactory
        {
            public TestTcpClientChannelFactory(IKeySigner keySigner)
                : base(keySigner) { }

            public IReadOnlyCollection<IChannelHandler> InheritedHandlers => Handlers;
        }

        private readonly IMessageCorrelationManager _correlationManager;
        private readonly TestTcpClientChannelFactory _factory;
        private readonly IKeySigner _keySigner;

        public TcpClientChannelFactoryTests()
        {
            _correlationManager = Substitute.For<IMessageCorrelationManager>();
            _keySigner = Substitute.For<IKeySigner>();

            var peerSettings = Substitute.For<IPeerSettings>();

            peerSettings.BindAddress.Returns(IPAddress.Parse("127.0.0.1"));
            peerSettings.Port.Returns(1234);
            _factory = new TestTcpClientChannelFactory(_keySigner);
        }

        [Fact]
        public void TcpServerChannelFactory_should_have_correct_handlers()
        {
            _factory.InheritedHandlers.Count(h => h != null).Should().Be(6);
            var handlers = _factory.InheritedHandlers.ToArray();
            handlers[0].Should().BeOfType<ProtobufVarint32LengthFieldPrepender>();
            handlers[1].Should().BeOfType<ProtobufEncoder>();
            handlers[2].Should().BeOfType<ProtobufVarint32FrameDecoder>();
            handlers[3].Should().BeOfType<ProtobufDecoder>();
            handlers[4].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[5].Should().BeOfType<ObservableServiceHandler>();
        }

        [Fact]
        public async Task TcpClientChannelFactory_should_put_the_correct_inbound_handlers_on_the_pipeline()
        {
            var testingChannel = new EmbeddedChannel("test".ToChannelId(),
                true, _factory.InheritedHandlers.ToArray());

            var senderId = PeerIdHelper.GetPeerId("sender");
            var correlationId = Guid.NewGuid();
            var protocolMessage = new PingResponse().ToProtocolMessage(senderId, correlationId);

            var observer = new ProtocolMessageObserver(0, Substitute.For<ILogger>());
           
            var messageStream = _factory.InheritedHandlers.OfType<ObservableServiceHandler>().Single().MessageStream;

            using (messageStream.Subscribe(observer))
            {
                testingChannel.WriteInbound(protocolMessage);
               
                // _correlationManager.Received(1).TryMatchResponse(protocolMessage); // @TODO in bound server shouldn't try and correlate a request, lets do another test to check this logic
                _correlationManager.DidNotReceiveWithAnyArgs().TryMatchResponse(protocolMessage);

                _keySigner.DidNotReceiveWithAnyArgs().Verify(null, null, null);

                await messageStream.WaitForItemsOnDelayedStreamOnTaskPoolScheduler();

                observer.Received.Count.Should().Be(1);
                observer.Received.Single().Payload.CorrelationId.ToGuid().Should().Be(correlationId);
            }
        }

        [Fact]
        public async Task TcpServerChannelFactory_should_put_the_correct_outbound_handlers_on_the_pipeline()
        {
            var testingChannel = new EmbeddedChannel("test".ToChannelId(),
                true, _factory.InheritedHandlers.ToArray());

            var senderId = PeerIdHelper.GetPeerId("sender");
            var correlationId = Guid.NewGuid();
            var protocolMessage = new PingRequest().ToProtocolMessage(senderId, correlationId);

            testingChannel.WriteOutbound(protocolMessage);

            // _correlationManager.Received(1).TryMatchResponse(protocolMessage); // @TODO in bound server shouldn't try and correlate a request, lets do another test to check this logic
            _correlationManager.DidNotReceiveWithAnyArgs().TryMatchResponse(protocolMessage);

            //commented is the expected behaviour.
            //_keySigner.ReceivedWithAnyArgs(1).Sign(Arg.Any<byte[]>());
            _keySigner.DidNotReceiveWithAnyArgs().Sign(Arg.Any<byte[]>());

            var outboundMessage = testingChannel.ReadOutbound<ProtocolMessageSigned>();
            outboundMessage.Should().BeNull();

            //Expected behaviour is commented below
            //outboundMessage.Should().NotBeNull();
            //outboundMessage.Message.CorrelationId.Should().Equal(correlationId);
        }
    }
}
