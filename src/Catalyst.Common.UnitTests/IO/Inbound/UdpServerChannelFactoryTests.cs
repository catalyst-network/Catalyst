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
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging.Gossip;
using Catalyst.Common.IO.Duplex;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Inbound.Handlers;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Inbound
{
    public class UdpServerChannelFactoryTests
    {
        private sealed class TestUdpServerChannelFactory : UdpServerChannelFactory
        {
            public TestUdpServerChannelFactory(IMessageCorrelationManager correlationManager,
                IGossipManager gossipManager,
                IKeySigner keySigner,
                IPeerSettings peerSettings)
                : base(correlationManager, gossipManager, keySigner, peerSettings) { }

            public IReadOnlyCollection<IChannelHandler> InheritedHandlers => Handlers;
        }

        private readonly IMessageCorrelationManager _correlationManager;
        private readonly IGossipManager _gossipManager;
        private readonly IKeySigner _keySigner;
        private readonly TestUdpServerChannelFactory _factory;

        public UdpServerChannelFactoryTests()
        {
            _correlationManager = Substitute.For<IMessageCorrelationManager>();
            _gossipManager = Substitute.For<IGossipManager>();
            _keySigner = Substitute.For<IKeySigner>();

            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.BindAddress.Returns(IPAddress.Parse("127.0.0.1"));
            peerSettings.Port.Returns(1234);
            _factory = new TestUdpServerChannelFactory(
                _correlationManager,
                _gossipManager,
                _keySigner,
                peerSettings);
        }

        [Fact]
        public void UdpServerChannelFactory_should_have_correct_handlers()
        {
            _factory.InheritedHandlers.Count(h => h != null).Should().Be(5);
            var handlers = _factory.InheritedHandlers.ToArray();
            handlers[0].Should().BeOfType<ProtoDatagramHandler>();
            handlers[1].Should().BeOfType<MessageSignerDuplex>();
            handlers[2].Should().BeOfType<GossipHandler>();
            handlers[3].Should().BeOfType<CorrelationHandler>();
            handlers[4].Should().BeOfType<ObservableServiceHandler>();
        }

        [Fact(Skip = "Reproduces the invalid message, will fix it soon")]
        public async Task UdpServerChannelFactory_should_put_the_correct_handlers_on_the_pipeline()
        {
            var testingChannel = new EmbeddedChannel("test".ToChannelId(),
                true, _factory.InheritedHandlers.ToArray());

            var senderId = PeerIdHelper.GetPeerId("sender");
            var correlationId = Guid.NewGuid();
            var protocolMessage = new PingRequest().ToProtocolMessage(senderId, correlationId);
            var datagram = protocolMessage.ToDatagram(new IPEndPoint(IPAddress.Loopback, 0));

            var observer = new ProtocolMessageObserver(0, Substitute.For<ILogger>());
           
            var messageStream = ((ObservableServiceHandler) _factory.InheritedHandlers.Last()).MessageStream;
            using (messageStream.Subscribe(observer))
            {
                testingChannel.WriteInbound(datagram);
                _correlationManager.DidNotReceiveWithAnyArgs().TryMatchResponse(null);
                await _gossipManager.DidNotReceiveWithAnyArgs().BroadcastAsync(null);
                _keySigner.DidNotReceiveWithAnyArgs().Verify(null, null, null);

                await messageStream.WaitForItemsOnDelayedStreamOnTaskPoolScheduler();

                observer.Received.Count.Should().Be(1);
                observer.Received.Single().Payload.CorrelationId.ToGuid().Should().Be(correlationId);
            }
        }
    }
}
