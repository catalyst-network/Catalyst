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

using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Abstractions.Rpc.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Handlers;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using Catalyst.TestUtils.Fakes;
using DotNetty.Buffers;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.Rpc.IO.Transport.Channels
{
    public sealed class RpcClientChannelFactoryTests
    {
        private TestScheduler _testScheduler;
        private UnitTests.Rpc.IO.Transport.Channels.RpcServerChannelFactoryTests.TestRpcServerChannelFactory _serverFactory;
        private EmbeddedChannel _serverChannel;
        private EmbeddedChannel _clientChannel;
        private IRpcMessageCorrelationManager _clientCorrelationManager;
        private FakeKeySigner _clientKeySigner;
        private IAuthenticationStrategy _authenticationStrategy;
        private IPeerIdValidator _peerIdValidator;
        private FakeKeySigner _serverKeySigner;
        private IRpcMessageCorrelationManager _serverCorrelationManager;

        [SetUp]
        public void Init()
        {
            _testScheduler = new TestScheduler();
            _serverCorrelationManager = Substitute.For<IRpcMessageCorrelationManager>();
            _serverKeySigner = Substitute.For<FakeKeySigner>();
            _serverKeySigner.CryptoContext.SignatureLength.Returns(64);

            _authenticationStrategy = Substitute.For<IAuthenticationStrategy>();

            _peerIdValidator = Substitute.For<IPeerIdValidator>();
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.NetworkType.Returns(NetworkType.Devnet);

            _serverFactory = new UnitTests.Rpc.IO.Transport.Channels.RpcServerChannelFactoryTests.TestRpcServerChannelFactory(
                _serverCorrelationManager,
                _serverKeySigner,
                _authenticationStrategy,
                _peerIdValidator,
                peerSettings,
                _testScheduler);

            _clientCorrelationManager = Substitute.For<IRpcMessageCorrelationManager>();
            _clientKeySigner = Substitute.For<FakeKeySigner>();
            _clientKeySigner.CryptoContext.SignatureLength.Returns(64);
            var clientFactory =
                new UnitTests.Rpc.IO.Transport.Channels.RpcClientChannelFactoryTests.TestRpcClientChannelFactory(
                    _clientKeySigner,
                    _clientCorrelationManager,
                    _peerIdValidator,
                    peerSettings,
                    _testScheduler);

            _serverChannel =
                new EmbeddedChannel("server".ToChannelId(), true, _serverFactory.InheritedHandlers.ToArray());

            _clientChannel =
                new EmbeddedChannel("client".ToChannelId(), true, clientFactory.InheritedHandlers.ToArray());
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public async Task
            RpcClientChannelFactory_Pipeline_Should_Produce_Request_Object_RpcServerChannelFactory_Can_Process_Into_Observable()
        {
            var recipient = PeerIdHelper.GetPeerId("recipient");
            var sender = PeerIdHelper.GetPeerId("sender");
            var signature = Substitute.For<ISignature>();
            signature.SignatureBytes.Returns(ByteUtil.GenerateRandomByteArray(new FfiWrapper().SignatureLength));

            _peerIdValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(true);

            _clientKeySigner.Sign(Arg.Any<byte[]>(), default).ReturnsForAnyArgs(signature);

            var correlationId = CorrelationId.GenerateCorrelationId();

            var protocolMessage = new GetPeerCountRequest().ToProtocolMessage(sender, correlationId);
            var dto = new MessageDto(
                protocolMessage,
                recipient
            );

            _clientChannel.WriteOutbound(dto);

            var sentBytes = _clientChannel.ReadOutbound<IByteBuffer>();

            // obviously
            sentBytes.Should().BeAssignableTo<IByteBuffer>();

            _clientCorrelationManager.ReceivedWithAnyArgs(1).AddPendingRequest(
                Arg.Is<CorrelatableMessage<ProtocolMessage>>(c =>
                    c.Content.CorrelationId.ToCorrelationId().Equals(correlationId)));

            _clientKeySigner.ReceivedWithAnyArgs(1).Sign(Arg.Is(signature.SignatureBytes), default);

            _serverKeySigner.Verify(
                    Arg.Any<ISignature>(),
                    Arg.Any<byte[]>(),
                    default
                )
               .ReturnsForAnyArgs(true);

            _authenticationStrategy.Authenticate(Arg.Any<PeerId>()).Returns(true);

            var observer = new ProtocolMessageObserver(0, Substitute.For<ILogger>());

            var messageStream = _serverFactory.InheritedHandlers.OfType<ObservableServiceHandler>().Single()
               .MessageStream;

            using (messageStream.Subscribe(observer))
            {
                _serverChannel.WriteInbound(sentBytes);
                _serverCorrelationManager.DidNotReceiveWithAnyArgs().TryMatchResponse(protocolMessage);

                _serverKeySigner.ReceivedWithAnyArgs(1).Verify(null, null, null);

                _testScheduler.Start();

                observer.Received.Count.Should().Be(1);
                observer.Received.Single().Payload.CorrelationId.ToCorrelationId().Id.Should().Be(correlationId.Id);
            }

            await _serverChannel.DisconnectAsync();
            await _clientChannel.DisconnectAsync();
        }
    }
}
