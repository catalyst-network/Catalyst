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
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using DotNetty.Transport.Channels.Sockets;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.IntegrationTests.P2P.IO.Transport.Channels
{
    public sealed class PeerClientChannelFactoryTests
    {
        private readonly UnitTests.P2P.IO.Transport.Channels.PeerClientChannelFactoryTests.TestPeerClientChannelFactory _clientFactory;
        private readonly EmbeddedChannel _serverChannel;
        private readonly EmbeddedChannel _clientChannel;
        private readonly IPeerMessageCorrelationManager _clientCorrelationManager;
        private readonly IKeySigner _clientKeySigner;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly IKeySigner _serverKeySigner;
        private readonly IPeerMessageCorrelationManager _serverCorrelationManager;
        private readonly ISignature _signature;

        public PeerClientChannelFactoryTests()
        {
            _serverCorrelationManager = Substitute.For<IPeerMessageCorrelationManager>();
            _serverKeySigner = Substitute.For<IKeySigner>();
            var broadcastManager = Substitute.For<IBroadcastManager>();

            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.BindAddress.Returns(IPAddress.Parse("127.0.0.1"));
            peerSettings.Port.Returns(1234);
            
            _peerIdValidator = Substitute.For<IPeerIdValidator>();

            var serverFactory = new UnitTests.P2P.IO.Transport.Channels.PeerServerChannelFactoryTests.TestPeerServerChannelFactory(
                _serverCorrelationManager,
                broadcastManager,
                _serverKeySigner,
                _peerIdValidator);

            _signature = Substitute.For<ISignature>();

            _clientCorrelationManager = Substitute.For<IPeerMessageCorrelationManager>();
            _clientKeySigner = Substitute.For<IKeySigner>();
           
            _clientFactory = new UnitTests.P2P.IO.Transport.Channels.PeerClientChannelFactoryTests.TestPeerClientChannelFactory(
                _clientKeySigner, 
                _clientCorrelationManager,
                _peerIdValidator);

            _serverChannel =
                new EmbeddedChannel("server".ToChannelId(), true, serverFactory.InheritedHandlers.ToArray());
            
            _clientChannel =
                new EmbeddedChannel("client".ToChannelId(), true, _clientFactory.InheritedHandlers.ToArray());
        }
        
        [Fact]
        public async Task
            PeerClientChannelFactory_Pipeline_Should_Produce_Request_Object_PeerClientChannelFactory_Can_Process()
        {
            var recipient = PeerIdentifierHelper.GetPeerIdentifier("recipient");
            var sender = PeerIdentifierHelper.GetPeerIdentifier("sender");
            _peerIdValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(true);

            _serverKeySigner.Sign(Arg.Any<byte[]>(), default).ReturnsForAnyArgs(_signature);
            
            var correlationId = CorrelationId.GenerateCorrelationId();

            var protocolMessage = new PingRequest().ToProtocolMessage(sender.PeerId, correlationId);
            var dto = new MessageDto<ProtocolMessage>(
                protocolMessage,
                sender,
                recipient,
                CorrelationId.GenerateCorrelationId()
            );

            _clientCorrelationManager.TryMatchResponse(Arg.Any<ProtocolMessage>()).Returns(true);
            
            _serverChannel.WriteOutbound(dto);
            var sentBytes = _serverChannel.ReadOutbound<DatagramPacket>();

            _serverCorrelationManager.ReceivedWithAnyArgs(1).AddPendingRequest(Arg.Any<CorrelatableMessage<ProtocolMessage>>());
            
            _serverKeySigner.ReceivedWithAnyArgs(1).Sign(Arg.Any<byte[]>(), default);
            
            _clientKeySigner.Verify(
                    Arg.Any<ISignature>(),
                    Arg.Any<byte[]>(), 
                    default
                )
               .ReturnsForAnyArgs(true);
            
            var observer = new ProtocolMessageObserver(0, Substitute.For<ILogger>());

            var messageStream = _clientFactory.InheritedHandlers.OfType<ObservableServiceHandler>().Single().MessageStream;
            
            using (messageStream.Subscribe(observer))
            {
                _clientChannel.WriteInbound(sentBytes);
                _clientChannel.ReadInbound<ProtocolMessage>();
                _clientCorrelationManager.DidNotReceiveWithAnyArgs().TryMatchResponse(Arg.Any<ProtocolMessage>());

                _clientKeySigner.ReceivedWithAnyArgs(1).Verify(null, null, null);

                await messageStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();
                observer.Received.Count.Should().Be(1);
                observer.Received.Single().Payload.CorrelationId.ToCorrelationId().Id.Should().Be(correlationId.Id);
            }
            
            await _serverChannel.DisconnectAsync();
            await _clientChannel.DisconnectAsync();
        }
    }
}
