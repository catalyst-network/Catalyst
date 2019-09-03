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
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Abstractions.Rpc.IO.Messaging.Correlation;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Handlers;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Buffers;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.IntegrationTests.Rpc.IO.Transport.Channels
{
    public sealed class NodeRpcServerChannelFactoryTests
    {
        private readonly TestScheduler _testScheduler;
        private readonly UnitTests.Rpc.IO.Transport.Channels.NodeRpcClientChannelFactoryTests.TestNodeRpcClientChannelFactory _clientFactory;
        private readonly EmbeddedChannel _serverChannel;
        private readonly EmbeddedChannel _clientChannel;
        private readonly IRpcMessageCorrelationManager _clientCorrelationManager;
        private readonly IKeySigner _clientKeySigner;
        private readonly IAuthenticationStrategy _authenticationStrategy;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly IKeySigner _serverKeySigner;
        private readonly IRpcMessageCorrelationManager _serverCorrelationManager;

        public NodeRpcServerChannelFactoryTests()
        {
            _testScheduler = new TestScheduler();
            _serverCorrelationManager = Substitute.For<IRpcMessageCorrelationManager>();
            _serverKeySigner = Substitute.For<IKeySigner>();

            var signatureContextProvider = Substitute.For<ISigningContextProvider>();
            signatureContextProvider.SignatureType.Returns(SignatureType.ProtocolPeer);

            signatureContextProvider.Network.Returns(Protocol.Common.Network.Devnet);
            _authenticationStrategy = Substitute.For<IAuthenticationStrategy>();

            _peerIdValidator = Substitute.For<IPeerIdValidator>();

            var serverFactory = new UnitTests.Rpc.IO.Transport.Channels.NodeRpcServerChannelFactoryTests.TestNodeRpcServerChannelFactory(
                _serverCorrelationManager,
                _serverKeySigner,
                _authenticationStrategy,
                _peerIdValidator,
                signatureContextProvider,
                _testScheduler);

            _clientCorrelationManager = Substitute.For<IRpcMessageCorrelationManager>();
            _clientKeySigner = Substitute.For<IKeySigner>();
           
            _clientFactory = new UnitTests.Rpc.IO.Transport.Channels.NodeRpcClientChannelFactoryTests.TestNodeRpcClientChannelFactory(
                _clientKeySigner, 
                _clientCorrelationManager,
                _peerIdValidator,
                signatureContextProvider,
                _testScheduler);

            _serverChannel =
                new EmbeddedChannel("server".ToChannelId(), true, serverFactory.InheritedHandlers.ToArray());
            
            _clientChannel =
                new EmbeddedChannel("client".ToChannelId(), true, _clientFactory.InheritedHandlers.ToArray());
        }

        [Fact]
        public async Task
            NodeRpcServerChannelFactory_Pipeline_Should_Produce_Response_Object_NodeRpcClientChannelFactory_Can_Process()
        {
            var recipient = PeerIdentifierHelper.GetPeerIdentifier("recipient");
            var sender = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var signature = Substitute.For<ISignature>();
            _peerIdValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(true);

            _serverKeySigner.Sign(Arg.Any<byte[]>(), default).ReturnsForAnyArgs(signature);
            
            var correlationId = CorrelationId.GenerateCorrelationId();

            var protocolMessage = new GetPeerCountResponse().ToProtocolMessage(sender.PeerId, correlationId);
            var dto = new MessageDto(
                protocolMessage,
                recipient
            );

            _clientCorrelationManager.TryMatchResponse(Arg.Any<ProtocolMessage>()).Returns(true);
            
            _serverChannel.WriteOutbound(dto);
            var sentBytes = _serverChannel.ReadOutbound<IByteBuffer>();

            _serverCorrelationManager.DidNotReceiveWithAnyArgs().AddPendingRequest(Arg.Any<CorrelatableMessage<ProtocolMessage>>());
            
            _serverKeySigner.ReceivedWithAnyArgs(1).Sign(Arg.Any<byte[]>(), default);
            
            _clientKeySigner.Verify(
                    Arg.Any<ISignature>(),
                    Arg.Any<byte[]>(), 
                    default)
               .ReturnsForAnyArgs(true);
            
            _authenticationStrategy.Authenticate(Arg.Any<IPeerIdentifier>()).Returns(true);
            
            var observer = new ProtocolMessageObserver(0, Substitute.For<ILogger>());

            var messageStream = _clientFactory.InheritedHandlers.OfType<ObservableServiceHandler>().Single().MessageStream;
            
            using (messageStream.Subscribe(observer))
            {
                _clientChannel.WriteInbound(sentBytes);
                _clientChannel.ReadInbound<ProtocolMessage>();
                _clientCorrelationManager.ReceivedWithAnyArgs(1).TryMatchResponse(Arg.Any<ProtocolMessage>());
                
                _clientKeySigner.ReceivedWithAnyArgs(1).Verify(null, null, null);

                _testScheduler.Start();

                observer.Received.Count.Should().Be(1);
                observer.Received.Single().Payload.CorrelationId.ToCorrelationId().Id.Should().Be(correlationId.Id);
            }
            
            await _serverChannel.DisconnectAsync();
            await _clientChannel.DisconnectAsync();
        }
    }
}
