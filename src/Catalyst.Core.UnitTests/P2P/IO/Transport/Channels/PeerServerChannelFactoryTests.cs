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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Handlers;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.P2P.IO.Transport.Channels;
using Catalyst.Core.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P.IO.Transport.Channels
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
                ISigningContextProvider signingContextProvider,
                TestScheduler testScheduler)
                : base(correlationManager, broadcastManager, keySigner, peerIdValidator, signingContextProvider,
                    testScheduler)
            {
                _handlers = HandlerGenerationFunction();
            }

            public IReadOnlyCollection<IChannelHandler> InheritedHandlers => _handlers;
        }

        private readonly TestScheduler _testScheduler;
        private readonly IPeerMessageCorrelationManager _correlationManager;
        private readonly IBroadcastManager _gossipManager;
        private readonly IKeySigner _keySigner;
        private readonly TestPeerServerChannelFactory _factory;
        private readonly PeerId _senderId;
        private readonly ICorrelationId _correlationId;
        private readonly byte[] _signature;

        public PeerServerChannelFactoryTests()
        {
            _testScheduler = new TestScheduler();
            _correlationManager = Substitute.For<IPeerMessageCorrelationManager>();
            _gossipManager = Substitute.For<IBroadcastManager>();
            _keySigner = Substitute.For<IKeySigner>();

            var signatureContext = Substitute.For<ISigningContextProvider>();
            signatureContext.Network.Returns(Protocol.Common.Network.Devnet);
            signatureContext.SignatureType.Returns(SignatureType.ProtocolPeer);

            var peerValidator = Substitute.For<IPeerIdValidator>();
            peerValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(true);

            _factory = new TestPeerServerChannelFactory(
                _correlationManager,
                _gossipManager,
                _keySigner,
                peerValidator,
                signatureContext,
                _testScheduler);
            _senderId = PeerIdHelper.GetPeerId("sender");
            _correlationId = CorrelationId.GenerateCorrelationId();
            _signature = ByteUtil.GenerateRandomByteArray(FFI.SignatureLength);
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

                _testScheduler.Start();

                observer.Received.Count.Should().Be(1);
                observer.Received.Single().Payload.CorrelationId.ToCorrelationId().Id.Should().Be(_correlationId.Id);
            }
        }

        [Fact]
        public void Observer_Exception_Should_Not_Stop_Correct_Messages_Reception()
        {
            var testingChannel = new EmbeddedChannel("testWithExceptions".ToChannelId(),
                true, _factory.InheritedHandlers.ToArray());

            var serverIdentifier = PeerIdentifierHelper.GetPeerIdentifier("server");
            using (var badHandler = new FailingRequestObserver(Substitute.For<ILogger>(), serverIdentifier))
            {
                var messageStream = GetObservableServiceHandler().MessageStream;
                badHandler.StartObserving(messageStream);

                Enumerable.Range(0, 10).ToList().ForEach(i => testingChannel.WriteInbound(GetSignedMessage()));

                _testScheduler.Start();

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
