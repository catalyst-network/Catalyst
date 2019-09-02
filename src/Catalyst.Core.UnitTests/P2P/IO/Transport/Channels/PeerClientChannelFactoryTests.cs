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
using System.Reactive.Concurrency;
using System.Threading.Tasks;
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
using DotNetty.Transport.Channels.Sockets;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P.IO.Transport.Channels
{
    public sealed class PeerClientChannelFactoryTests
    {
        public sealed class TestPeerClientChannelFactory : PeerClientChannelFactory
        {
            private readonly List<IChannelHandler> _handlers;

            public TestPeerClientChannelFactory(IKeySigner keySigner,
                IPeerMessageCorrelationManager correlationManager,
                IPeerIdValidator peerIdValidator,
                ISigningContextProvider signingContextProvider,
                IScheduler scheduler)
                : base(keySigner, correlationManager, peerIdValidator, signingContextProvider, scheduler)
            {
                _handlers = HandlerGenerationFunction();
            }

            public IReadOnlyCollection<IChannelHandler> InheritedHandlers => _handlers;
        }

        private readonly TestScheduler _testScheduler;
        private readonly IPeerMessageCorrelationManager _correlationManager;
        private readonly IBroadcastManager _gossipManager;
        private readonly IKeySigner _keySigner;
        private readonly TestPeerClientChannelFactory _factory;

        public PeerClientChannelFactoryTests()
        {
            _testScheduler = new TestScheduler();
            _correlationManager = Substitute.For<IPeerMessageCorrelationManager>();
            _gossipManager = Substitute.For<IBroadcastManager>();
            _keySigner = Substitute.For<IKeySigner>();

            var signingContext = Substitute.For<ISigningContextProvider>();
            signingContext.Network.Returns(Protocol.Common.Network.Devnet);
            signingContext.SignatureType.Returns(SignatureType.ProtocolPeer);

            var peerValidator = Substitute.For<IPeerIdValidator>();
            peerValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(true);

            _factory = new TestPeerClientChannelFactory(
                _keySigner,
                _correlationManager,
                peerValidator,
                signingContext,
                _testScheduler);
        }

        [Fact]
        public void PeerClientChannelFactory_should_have_correct_handlers()
        {
            _factory.InheritedHandlers.Count(h => h != null).Should().Be(6);
            var handlers = _factory.InheritedHandlers.ToArray();
            handlers[0].Should().BeOfType<FlushPipelineHandler<DatagramPacket>>();
            handlers[1].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[2].Should().BeOfType<PeerIdValidationHandler>();
            handlers[3].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[4].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[5].Should().BeOfType<ObservableServiceHandler>();
        }

        [Fact(Skip = "true")]
        public async Task PeerClientChannelFactory_should_put_the_correct_handlers_on_the_inbound_pipeline()
        {
            var testingChannel = new EmbeddedChannel("test".ToChannelId(),
                true, _factory.InheritedHandlers.ToArray());

            var senderId = PeerIdHelper.GetPeerId("sender");
            var correlationId = CorrelationId.GenerateCorrelationId();
            var protocolMessage = new PingRequest().ToProtocolMessage(senderId, correlationId);
            var signature = ByteUtil.GenerateRandomByteArray(FFI.SignatureLength);

            var signedMessage = new ProtocolMessageSigned
            {
                Message = protocolMessage,
                Signature = signature.ToByteString()
            };

            _keySigner.Verify(Arg.Is<ISignature>(s => s.SignatureBytes.SequenceEqual(signature)), Arg.Any<byte[]>(),
                    default)
               .ReturnsForAnyArgs(true);

            var observer = new ProtocolMessageObserver(0, Substitute.For<ILogger>());

            var messageStream = ((ObservableServiceHandler) _factory.InheritedHandlers.Last()).MessageStream;

            using (messageStream.Subscribe(observer))
            {
                testingChannel.WriteInbound(signedMessage);
                _correlationManager.DidNotReceiveWithAnyArgs().TryMatchResponse(protocolMessage);
                await _gossipManager.DidNotReceiveWithAnyArgs().BroadcastAsync(null);
                _keySigner.ReceivedWithAnyArgs(1).Verify(null, null, null);

                _testScheduler.Start();

                observer.Received.Count.Should().Be(1);
                observer.Received.Single().Payload.CorrelationId.ToCorrelationId().Id.Should().Be(correlationId.Id);
            }
        }
    }
}
