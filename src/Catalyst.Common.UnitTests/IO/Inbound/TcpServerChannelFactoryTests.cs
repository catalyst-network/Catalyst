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
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Inbound
{
    public class TcpServerChannelFactoryTests
    {
        private sealed class TestTcpServerChannelFactory : TcpServerChannelFactory
        {
            public TestTcpServerChannelFactory(ICorrelationManager correlationManager,
                IPeerSettings peerSettings)
                : base(correlationManager, peerSettings) { }

            public IReadOnlyCollection<IChannelHandler> InheritedHandlers => Handlers;
        }

        private readonly ICorrelationManager _correlationManager;
        private readonly TestTcpServerChannelFactory _factory;

        public TcpServerChannelFactoryTests()
        {
            _correlationManager = Substitute.For<ICorrelationManager>();

            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.BindAddress.Returns(IPAddress.Parse("127.0.0.1"));
            peerSettings.Port.Returns(1234);
            _factory = new TestTcpServerChannelFactory(
                _correlationManager,
                peerSettings);
        }

        [Fact]
        public void TcpServerChannelFactory_should_have_correct_handlers()
        {
            _factory.InheritedHandlers.Count(h => h != null).Should().Be(6);
            var handlers = _factory.InheritedHandlers.ToArray();
            handlers[0].Should().BeOfType<ProtobufVarint32FrameDecoder>();
            handlers[1].Should().BeOfType<ProtobufDecoder>();
            handlers[2].Should().BeOfType<ProtobufVarint32LengthFieldPrepender>();
            handlers[3].Should().BeOfType<ProtobufEncoder>();
            handlers[4].Should().BeOfType<CorrelationHandler>();
            handlers[5].Should().BeOfType<ObservableServiceHandler>();
        }

        [Fact]
        public async Task TcpServerChannelFactory_should_put_the_correct_handlers_on_the_pipeline()
        {
            var testingChannel = new EmbeddedChannel("test".ToChannelId(),
                true, _factory.InheritedHandlers.ToArray());

            var senderId = PeerIdHelper.GetPeerId("sender");
            var correlationId = Guid.NewGuid();
            var protocolMessage = new PingRequest().ToAnySigned(senderId, correlationId);

            var observer = new ProtocolMessageObserver(0, Substitute.For<ILogger>());
           
            var messageStream = ((ObservableServiceHandler) _factory.InheritedHandlers.Last()).MessageStream;
            using (messageStream.Subscribe(observer))
            {
                messageStream.Publish().Connect();
                testingChannel.WriteInbound(protocolMessage);
                _correlationManager.Received(1).TryMatchResponse(protocolMessage);
                
                await messageStream.WaitForItemsOnDelayedStreamOnTaskPoolScheduler();

                observer.Received.Count.Should().Be(1);
                observer.Received.Single().Payload.CorrelationId.ToGuid().Should().Be(correlationId);
            }
        }
    }
}
