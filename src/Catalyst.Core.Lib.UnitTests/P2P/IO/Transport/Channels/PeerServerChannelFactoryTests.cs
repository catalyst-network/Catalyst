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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Util;
using Catalyst.Core.Lib.P2P.IO.Transport.Channels;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P.IO.Transport.Channels
{
    public sealed class PeerServerChannelFactoryTests
    {
        public sealed class TestPeerServerChannelFactory : PeerServerChannelFactory
        {
            private readonly List<IChannelHandler> _handlers;

            public TestPeerServerChannelFactory(IPeerMessageCorrelationManager correlationManager,
                IBroadcastManager broadcastManager,
                IKeySigner keySigner,
                IPeerIdValidator peerIdValidator,
                IPeerSettings peerSettings)
                : base(correlationManager, broadcastManager, keySigner, peerIdValidator, peerSettings)
            {
                _handlers = HandlerGenerationFunction();
            }

            public IReadOnlyCollection<IChannelHandler> InheritedHandlers => _handlers;
        }

        private readonly IPeerMessageCorrelationManager _correlationManager;
        private readonly IBroadcastManager _gossipManager;
        private readonly IKeySigner _keySigner;
        private readonly TestPeerServerChannelFactory _factory;
        private readonly PeerId _senderId;
        private readonly ICorrelationId _correlationId;
        private readonly byte[] _signature;
        private readonly IPeerSettings _peerSettings;

        public PeerServerChannelFactoryTests()
        {
            _correlationManager = Substitute.For<IPeerMessageCorrelationManager>();
            _gossipManager = Substitute.For<IBroadcastManager>();
            _keySigner = Substitute.For<IKeySigner>();

            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.BindAddress.Returns(IPAddress.Parse("127.0.0.1"));
            peerSettings.Port.Returns(1234);
            peerSettings.Network.Returns(Network.Devnet);

            var peerValidator = Substitute.For<IPeerIdValidator>();
            peerValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(true);

            _factory = new TestPeerServerChannelFactory(
                _correlationManager,
                _gossipManager,
                _keySigner,
                peerValidator,
                _peerSettings);

            _senderId = PeerIdHelper.GetPeerId("sender");
            _correlationId = CorrelationId.GenerateCorrelationId();
            _signature = ByteUtil.GenerateRandomByteArray(Cryptography.BulletProofs.Wrapper.FFI.SignatureLength);
            _keySigner.Verify(Arg.Any<ISignature>(), Arg.Any<byte[]>(), default)
               .ReturnsForAnyArgs(true);
        }

        [Fact]
        public void PeerServerChannelFactory_should_have_correct_handlers()
        {
            _factory.InheritedHandlers.Count(h => h != null).Should().Be(7);
            var handlers = _factory.InheritedHandlers.ToArray();
            handlers[0].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[1].Should().BeOfType<PeerIdValidationHandler>();
            handlers[2].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[3].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[4].Should().BeOfType<BroadcastHandler>();
            handlers[5].Should().BeOfType<ObservableServiceHandler>();
            handlers[6].Should().BeOfType<BroadcastCleanupHandler>();
        }

        [Fact]
        public async Task PeerServerChannelFactory_should_put_the_correct_handlers_on_the_inbound_pipeline()
        {
            var testingChannel = new EmbeddedChannel("test".ToChannelId(),
                true, _factory.InheritedHandlers.ToArray());

            var protocolMessage = new PingRequest().ToProtocolMessage(_senderId, _correlationId);

            var signedMessage = new ProtocolMessageSigned
            {
                Message = protocolMessage,
                Signature = _signature.ToByteString()
            };

            var observer = new ProtocolMessageObserver(0, Substitute.For<ILogger>());
           
            var messageStream = GetObservableServiceHandler().MessageStream;
            
            using (messageStream.Subscribe(observer))
            {
                testingChannel.WriteInbound(signedMessage);
                _correlationManager.DidNotReceiveWithAnyArgs().TryMatchResponse(protocolMessage);
                await _gossipManager.DidNotReceiveWithAnyArgs().BroadcastAsync(null);

                _keySigner.ReceivedWithAnyArgs(1).Verify(null, null, null);

                await messageStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();

                observer.Received.Count.Should().Be(1);
                observer.Received.Single().Payload.CorrelationId.ToCorrelationId().Id.Should().Be(_correlationId.Id);
            }
        }
         
        [Fact]
        public async Task Observer_Exception_Should_Not_Stop_Correct_Messages_Reception()
        {
            var testingChannel = new EmbeddedChannel("testWithExceptions".ToChannelId(),
                true, _factory.InheritedHandlers.ToArray());
            
            var serverIdentifier = PeerIdentifierHelper.GetPeerIdentifier("server");
            using (var badHandler = new FailingRequestObserver(Substitute.For<ILogger>(), serverIdentifier))
            {
                var messageStream = GetObservableServiceHandler().MessageStream;
                badHandler.StartObserving(messageStream);

                Enumerable.Range(0, 10).AsParallel().ForAll(i => testingChannel.WriteInbound(GetSignedMessage()));

                await TaskHelper.WaitForAsync(
                    () => testingChannel.OutboundMessages.Count >= 5, 
                    TimeSpan.FromSeconds(5));

                await messageStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync().ConfigureAwait(false);

                badHandler.Counter.Should().Be(10);
            }
        }

        private ProtocolMessageSigned GetSignedMessage()
        {
            var protocolMessage = new PeerNeighborsRequest()
               .ToProtocolMessage(_senderId, CorrelationId.GenerateCorrelationId());

            var signedMessage = new ProtocolMessageSigned
            {
                Message = protocolMessage,
                Signature = _signature.ToByteString()
            };
            return signedMessage;
        }

        private ObservableServiceHandler GetObservableServiceHandler()
        {
            return _factory.InheritedHandlers
               .OfType<ObservableServiceHandler>().FirstOrDefault();
        }
    }
}
