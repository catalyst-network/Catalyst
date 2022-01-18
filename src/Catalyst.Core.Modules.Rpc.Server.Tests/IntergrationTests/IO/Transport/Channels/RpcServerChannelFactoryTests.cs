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
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Protocol.Network;
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
using MultiFormats;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Messaging.Dto;
using Catalyst.Modules.Network.Dotnetty.IO.Handlers;
using Catalyst.Modules.Network.Dotnetty.IO.Messaging.Dto;
using Catalyst.Core.Modules.Rpc.Client.Tests.UnitTests.IO.Transport.Channels;
using static Catalyst.Core.Modules.Rpc.Server.Tests.UnitTests.IO.Transport.Channels.RpcServerChannelFactoryTests;

namespace Catalyst.Core.Modules.Rpc.Server.Tests.IntegrationTests.IO.Transport.Channels
{
    public sealed class RpcServerChannelFactoryTests
    {
        private readonly TestScheduler _testScheduler;
        private readonly RpcClientChannelFactoryTests.TestRpcClientChannelFactory _clientFactory;
        private readonly EmbeddedChannel _serverChannel;
        private readonly EmbeddedChannel _clientChannel;
        private readonly IRpcMessageCorrelationManager _clientCorrelationManager;
        private readonly FakeKeySigner _clientKeySigner;
        private readonly IAuthenticationStrategy _authenticationStrategy;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly FakeKeySigner _serverKeySigner;
        private readonly IRpcMessageCorrelationManager _serverCorrelationManager;

        public RpcServerChannelFactoryTests()
        {
            _testScheduler = new TestScheduler();
            _serverCorrelationManager = Substitute.For<IRpcMessageCorrelationManager>();
            _serverKeySigner = Substitute.For<FakeKeySigner>();
            _serverKeySigner.CryptoContext.SignatureLength.Returns(64);

            _authenticationStrategy = Substitute.For<IAuthenticationStrategy>();

            _peerIdValidator = Substitute.For<IPeerIdValidator>();
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.NetworkType.Returns(NetworkType.Devnet);

            TestRpcServerChannelFactory serverFactory = new(
                _serverCorrelationManager,
                _serverKeySigner,
                _authenticationStrategy,
                _peerIdValidator,
                peerSettings,
                _testScheduler);

            _clientCorrelationManager = Substitute.For<IRpcMessageCorrelationManager>();
            _clientKeySigner = Substitute.For<FakeKeySigner>();
            _clientKeySigner.CryptoContext.SignatureLength.Returns(64);

            _clientFactory = new RpcClientChannelFactoryTests.TestRpcClientChannelFactory(
                _clientKeySigner,
                _clientCorrelationManager,
                _peerIdValidator,
                peerSettings,
                _testScheduler);

            _serverChannel =
                new EmbeddedChannel("server".ToChannelId(), true, serverFactory.InheritedHandlers.ToArray());

            _clientChannel =
                new EmbeddedChannel("client".ToChannelId(), true, _clientFactory.InheritedHandlers.ToArray());
        }

        [Test]

        public async Task
            RpcServerChannelFactory_Pipeline_Should_Produce_Response_Object_RpcClientChannelFactory_Can_Process()
        {
            var recipient = MultiAddressHelper.GetAddress("recipient");
            var sender = MultiAddressHelper.GetAddress("sender");
            var signature = Substitute.For<ISignature>();
            _peerIdValidator.ValidatePeerIdFormat(Arg.Any<MultiAddress>()).Returns(true);

            _serverKeySigner.Sign(Arg.Any<byte[]>(), default).ReturnsForAnyArgs(signature);

            var correlationId = CorrelationId.GenerateCorrelationId();

            var protocolMessage = new GetPeerCountResponse().ToProtocolMessage(sender, correlationId);
            MessageDto dto = new(
                protocolMessage,
                recipient
            );

            _clientCorrelationManager.TryMatchResponse(Arg.Any<ProtocolMessage>()).Returns(true);

            _serverChannel.WriteOutbound(dto);
            var sentBytes = _serverChannel.ReadOutbound<IByteBuffer>();

            _serverCorrelationManager.DidNotReceiveWithAnyArgs().AddPendingRequest(Arg.Any<CorrelatableMessage<ProtocolMessage>>());

            _serverKeySigner.ReceivedWithAnyArgs(1)
               .Sign(Arg.Any<byte[]>(), default);

            _clientKeySigner.Verify(
                    Arg.Any<ISignature>(),
                    Arg.Any<byte[]>(),
                    default)
               .ReturnsForAnyArgs(true);

            _authenticationStrategy.Authenticate(Arg.Any<MultiAddress>()).Returns(true);

            ProtocolMessageObserver<IObserverDto<ProtocolMessage>> observer = new(0, Substitute.For<ILogger>());

            var messageStream = _clientFactory.InheritedHandlers.OfType<RpcObservableServiceHandler>().Single().MessageStream;

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
