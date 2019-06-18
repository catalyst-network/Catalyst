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
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.RPC.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Common.UnitTests.IO
{
    public class TcpClientAndServerCommunicationTests
    {
        private readonly IMessageCorrelationManager _serverCorrelationManager;
        private readonly IKeySigner _serverKeySigner;
        private readonly TcpServerChannelFactoryTests.TestTcpServerChannelFactory _serverFactory;
        private readonly IKeySigner _clientKeySigner;
        private readonly TcpClientChannelFactoryTests.TestTcpClientChannelFactory _clientFactory;

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

            var sent = clientChannel.ReadOutbound<ProtocolMessage>();
            sent.CorrelationId.ToGuid().Should().Be(guid);

            var serverObserver = new ProtocolMessageObserver(0, Substitute.For<ILogger>());
            var messageStream = _serverFactory.InheritedHandlers.OfType<ObservableServiceHandler>().Single().MessageStream;

            var peerRepo = Substitute.For<IRepository<Peer>>();
            peerRepo.GetAll().Returns(Enumerable.Repeat(new Peer(), 12));
            var peerCountObserver = new PeerCountRequestObserver(
                new PeerIdentifier(serverId), peerRepo, Substitute.For<ILogger>());

            peerCountObserver.StartObserving(messageStream);
            using (messageStream.Subscribe(serverObserver))
            {
                serverChannel.WriteInbound(sent);
                await messageStream.WaitForItemsOnDelayedStreamOnTaskPoolScheduler();

                serverObserver.Received.Count.Should().Be(1);
                serverObserver.Received.Single().Payload.CorrelationId.ToGuid().Should().Be(guid);

                //peerCountObserver.ChannelHandlerContext.Received()
                //finish that tomorrow  
            }
        }
    }
}
