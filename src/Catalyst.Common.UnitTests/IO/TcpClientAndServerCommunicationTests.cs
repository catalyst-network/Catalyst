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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging.Dto;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.P2P;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Node.Core.RPC.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Common.UnitTests.IO
{
    public class TcpClientAndServerCommunicationTests : IDisposable
    {
        private readonly IMessageCorrelationManager _serverCorrelationManager;
        private readonly IKeySigner _serverKeySigner;
        private readonly TcpServerChannelFactoryTests.TestTcpServerChannelFactory _serverFactory;
        private readonly IKeySigner _clientKeySigner;
        private readonly TcpClientChannelFactoryTests.TestTcpClientChannelFactory _clientFactory;
        private IRepository<Peer> _peerRepository;
        private PeerCountRequestObserver _peerCountObserver;
        private TestMessageObserver<GetPeerCountResponse> _clientObserver;

        public TcpClientAndServerCommunicationTests()
        {
            _serverCorrelationManager = Substitute.For<IMessageCorrelationManager>();
            _serverKeySigner = Substitute.For<IKeySigner>();

            var peerSettings = Substitute.For<IPeerSettings>();

            peerSettings.BindAddress.Returns(IPAddress.Parse("127.0.0.1"));
            peerSettings.Port.Returns(1234);
            _serverFactory = new TcpServerChannelFactoryTests.TestTcpServerChannelFactory(
                _serverCorrelationManager,
                peerSettings,
                _serverKeySigner);

            _clientKeySigner = Substitute.For<IKeySigner>();
            _clientFactory = new TcpClientChannelFactoryTests.TestTcpClientChannelFactory(_clientKeySigner);
        }

        [Fact]
        public async Task Server_and_client_should_process_Requests_from_end_to_end()
        {
            var serverId = PeerIdHelper.GetPeerId("server");
            var clientId = PeerIdHelper.GetPeerId("client");
            var guid = Guid.NewGuid();

            var serverChannel = new EmbeddedChannel("server".ToChannelId(), true, _serverFactory.InheritedHandlers.ToArray());
            var clientChannel = new EmbeddedChannel("client".ToChannelId(), true, _clientFactory.InheritedHandlers.ToArray());

            var initialRequest = new GetPeerCountRequest().ToProtocolMessage(clientId, guid);
            clientChannel.WriteOutbound(initialRequest);

            var sent = clientChannel.ReadOutbound<ProtocolMessageSigned>();
            //sent.Signature.Should().NotBeNullOrEmpty();
            //sent.Message.CorrelationId.ToGuid().Should().Be(guid);
            //var unwrappedMessage = sent.Message.FromProtocolMessage<GetPeerCountRequest>();
            //unwrappedMessage.Should().NotBeNull();

            var serverMessageStream = _serverFactory.InheritedHandlers.OfType<ObservableServiceHandler>().Single().MessageStream;

            //We can do that and have a HandleRequest mocked
            //var serverObserver = new TestMessageObserver<GetPeerCountRequest>(Substitute.For<ILogger>());
            //_serverSubscription = messageStream.Subscribe(serverObserver))

            //but maybe we want to take a real one and let its implementation do the posting back on the server outbound channel
            _peerRepository = Substitute.For<IRepository<Peer>>();
            var expectedPeerCount = 12; 
            _peerRepository.GetAll().Returns(Enumerable.Repeat(new Peer(), expectedPeerCount));
            _peerCountObserver = new PeerCountRequestObserver(
                new PeerIdentifier(serverId), _peerRepository, Substitute.For<ILogger>());

            _peerCountObserver.StartObserving(serverMessageStream);
            
            serverChannel.WriteInbound(sent);
            //await serverMessageStream.WaitForItemsOnDelayedStreamOnTaskPoolScheduler();

            //serverObserver.SubstituteObserver.Received(1).OnNext(Arg.Any<GetPeerCountRequest>());
            //serverObserver.HandleRequest(Arg.Any<IProtocolMessageDto<ProtocolMessage>>()).Received(1);
            //serverObserver.ChannelHandlerContext.Received(1).WriteAndFlushAsync(Arg.Any<IMessage>());
            //var rawResponse = serverObserver.ChannelHandlerContext.ReceivedCalls().Single().GetArguments().Single();

            //send the response back on the outbound channel
            //serverChannel.WriteOutbound(rawResponse);
            //_serverKeySigner.Received(1).Sign(Arg.Any<byte[]>());
            var response = serverChannel.ReadOutbound<ProtocolMessageSigned>();
            //response.Should().NotBeNull();
            //response.Signature.Should().NotBeEmpty();

            var clientMessageStream = _clientFactory.InheritedHandlers.OfType<ObservableServiceHandler>().Single().MessageStream;
            _clientObserver = new TestMessageObserver<GetPeerCountResponse>(Substitute.For<ILogger>());
            _clientObserver.StartObserving(clientMessageStream);

            //clientChannel.WriteInbound(response);
            //await clientMessageStream.WaitForItemsOnDelayedStreamOnTaskPoolScheduler();

            //_clientKeySigner.Received(1).Verify(Arg.Any<IPublicKey>(), Arg.Any<byte[]>(), Arg.Any<ISignature>());
            //_clientObserver.SubstituteObserver.Received(1).OnNext(Arg.Is<GetPeerCountResponse>(c => c.PeerCount == expectedPeerCount));

            await serverChannel.DisconnectAsync();
            await clientChannel.DisconnectAsync();
        }

        public void Dispose()
        {
            _peerRepository.Dispose();
            _peerCountObserver.Dispose();
            
            //_serverSubscription.Dispose();
            _clientObserver.Dispose();
        }
    }
}
