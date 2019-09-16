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
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Abstractions.Rpc.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Handlers;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using Catalyst.TestUtils.Protocol;
using DotNetty.Buffers;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.Rpc.IO.Transport.Channels
{
    public sealed class NodeRpcClientChannelFactoryTests
    {
        private readonly TestScheduler _testScheduler;
        private readonly UnitTests.Rpc.IO.Transport.Channels.NodeRpcServerChannelFactoryTests.TestNodeRpcServerChannelFactory _serverFactory;
        private readonly EmbeddedChannel _serverChannel;
        private readonly EmbeddedChannel _clientChannel;
        private readonly IRpcMessageCorrelationManager _clientCorrelationManager;
        private readonly IKeySigner _clientKeySigner;
        private readonly IAuthenticationStrategy _authenticationStrategy;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly IKeySigner _serverKeySigner;
        private readonly IRpcMessageCorrelationManager _serverCorrelationManager;

        public NodeRpcClientChannelFactoryTests()
        {
            _testScheduler = new TestScheduler();
            _serverCorrelationManager = Substitute.For<IRpcMessageCorrelationManager>();
            _serverKeySigner = Substitute.For<IKeySigner>();
            _serverKeySigner.CryptoContext.SignatureLength.Returns(64);

            _authenticationStrategy = Substitute.For<IAuthenticationStrategy>();

            _peerIdValidator = Substitute.For<IPeerIdValidator>();

            _serverFactory = new UnitTests.Rpc.IO.Transport.Channels.NodeRpcServerChannelFactoryTests.TestNodeRpcServerChannelFactory(
                _serverCorrelationManager,
                _serverKeySigner,
                _authenticationStrategy,
                _peerIdValidator,
                DevNetPeerSigningContextProvider.Instance,
                _testScheduler);

            _clientCorrelationManager = Substitute.For<IRpcMessageCorrelationManager>();
            _clientKeySigner = Substitute.For<IKeySigner>();
            _clientKeySigner.CryptoContext.SignatureLength.Returns(64);
            var clientFactory =
                new UnitTests.Rpc.IO.Transport.Channels.NodeRpcClientChannelFactoryTests.TestNodeRpcClientChannelFactory(
                    _clientKeySigner,
                    _clientCorrelationManager,
                    _peerIdValidator,
                    DevNetPeerSigningContextProvider.Instance,
                    _testScheduler);

            _serverChannel =
                new EmbeddedChannel("server".ToChannelId(), true, _serverFactory.InheritedHandlers.ToArray());

            _clientChannel =
                new EmbeddedChannel("client".ToChannelId(), true, clientFactory.InheritedHandlers.ToArray());
        }

        [Fact]
        public async Task
            NodeRpcClientChannelFactory_Pipeline_Should_Produce_Request_Object_NodeRpcServerChannelFactory_Can_Process_Into_Observable()
        {
            var recipient = PeerIdentifierHelper.GetPeerIdentifier("recipient");
            var sender = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var signature = Substitute.For<ISignature>();
            signature.SignatureBytes.Returns(ByteUtil.GenerateRandomByteArray(FFI.SignatureLength));

            _peerIdValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(true);

            _clientKeySigner.Sign(Arg.Any<byte[]>(), default).ReturnsForAnyArgs(signature);

            var correlationId = CorrelationId.GenerateCorrelationId();

            var protocolMessage = new GetPeerCountRequest().ToProtocolMessage(sender.PeerId, correlationId);
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

            _authenticationStrategy.Authenticate(Arg.Any<IPeerIdentifier>()).Returns(true);

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
