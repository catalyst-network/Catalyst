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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Abstractions.Rpc.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Codecs;
using Catalyst.Core.Lib.IO.Handlers;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Rpc.Server.Transport.Channels;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using Catalyst.TestUtils.Fakes;
using DotNetty.Buffers;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using MultiFormats;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Transport.Channels
{
    public sealed class RpcServerChannelFactoryTests
    {
        public sealed class TestRpcServerChannelFactory : RpcServerChannelFactory
        {
            private readonly List<IChannelHandler> _handlers;

            public TestRpcServerChannelFactory(IRpcMessageCorrelationManager correlationManager,
                IKeySigner keySigner,
                IAuthenticationStrategy authenticationStrategy,
                IPeerIdValidator peerIdValidator,
                IPeerSettings peerSettings,
                TestScheduler testScheduler)
                : base(correlationManager, keySigner, authenticationStrategy, peerIdValidator, peerSettings, testScheduler)
            {
                _handlers = HandlerGenerationFunction();
            }

            public IReadOnlyCollection<IChannelHandler> InheritedHandlers => _handlers;
        }

        private TestScheduler _testScheduler;
        private IRpcMessageCorrelationManager _correlationManager;
        private TestRpcServerChannelFactory _factory;
        private FakeKeySigner _keySigner;

        [SetUp]
        public void Init()
        {
            _testScheduler = new TestScheduler();
            _correlationManager = Substitute.For<IRpcMessageCorrelationManager>();
            _keySigner = Substitute.For<FakeKeySigner>();
            _keySigner.CryptoContext.SignatureLength.Returns(64);

            var authenticationStrategy = Substitute.For<IAuthenticationStrategy>();
            authenticationStrategy.Authenticate(Arg.Any<MultiAddress>()).Returns(true);

            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.NetworkType.Returns(NetworkType.Devnet);

            var peerIdValidator = Substitute.For<IPeerIdValidator>();
            peerIdValidator.ValidatePeerIdFormat(Arg.Any<MultiAddress>()).Returns(true);
            _factory = new TestRpcServerChannelFactory(
                _correlationManager,
                _keySigner,
                authenticationStrategy,
                peerIdValidator,
                peerSettings,
                _testScheduler);
        }

        [Test]
        public void RpcServerChannelFactory_should_have_correct_handlers()
        {
            _factory.InheritedHandlers.Count(h => h != null).Should().Be(10);
            var handlers = _factory.InheritedHandlers.ToArray();
            handlers[0].Should().BeOfType<ProtobufVarint32FrameDecoder>();
            handlers[1].Should().BeOfType<ProtobufDecoder>();
            handlers[2].Should().BeOfType<ProtobufVarint32LengthFieldPrepender>();
            handlers[3].Should().BeOfType<ProtobufEncoder>();
            handlers[4].Should().BeOfType<AuthenticationHandler>();
            handlers[5].Should().BeOfType<PeerIdValidationHandler>();
            handlers[6].Should().BeOfType<AddressedEnvelopeToIMessageEncoder>();
            handlers[7].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[8].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[9].Should().BeOfType<ObservableServiceHandler>();
        }

        [Test]
        public void RpcServerChannelFactory_should_put_the_correct_inbound_handlers_on_the_pipeline()
        {
            var testingChannel = new EmbeddedChannel("test".ToChannelId(),
                true, _factory.InheritedHandlers.ToArray());

            var senderId = PeerIdHelper.GetPeerId("sender");
            var correlationId = CorrelationId.GenerateCorrelationId();
            var signatureBytes = ByteUtil.GenerateRandomByteArray(new FfiWrapper().SignatureLength);
            _keySigner.Verify(Arg.Any<ISignature>(), Arg.Any<byte[]>(), Arg.Any<SigningContext>())
               .Returns(true);
            var protocolMessage = new PingRequest().ToSignedProtocolMessage(senderId, signatureBytes, correlationId: correlationId);

            var observer = new ProtocolMessageObserver(0, Substitute.For<ILogger>());

            var messageStream = _factory.InheritedHandlers.OfType<ObservableServiceHandler>()
               .Single().MessageStream;
            
            using (messageStream.Subscribe(observer))
            {
                testingChannel.WriteInbound(protocolMessage);
                _correlationManager.DidNotReceiveWithAnyArgs().TryMatchResponse(protocolMessage);
                _keySigner.ReceivedWithAnyArgs(1).Verify(null, null, null);
                _testScheduler.Start();
                observer.Received.Count.Should().Be(1);
                observer.Received.Single().Payload.CorrelationId.ToCorrelationId().Id.Should().Be(correlationId.Id);
            }
        }

        [Test]
        public void RpcServerChannelFactory_should_put_the_correct_outbound_handlers_on_the_pipeline()
        {
            var testingChannel = new EmbeddedChannel("test".ToChannelId(),
                true, _factory.InheritedHandlers.ToArray());
            
            var senderId = PeerIdHelper.GetPeerId("sender");
            var correlationId = CorrelationId.GenerateCorrelationId();
            var protocolMessage = new PingResponse().ToProtocolMessage(senderId, correlationId);

            testingChannel.WriteOutbound(protocolMessage);

            _correlationManager.DidNotReceiveWithAnyArgs().TryMatchResponse(protocolMessage);
            
            _keySigner.DidNotReceiveWithAnyArgs().Sign(Arg.Any<byte[]>(), default);

            testingChannel.ReadOutbound<IByteBuffer>();
        }
    }
}
