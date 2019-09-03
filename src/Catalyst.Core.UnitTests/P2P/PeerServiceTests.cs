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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.EventLoop;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.UnitTests.P2P
{
    public sealed class PeerServiceTests : SelfAwareTestBase, IDisposable
    {
        private readonly ICorrelationId _guid;
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _pid;
        private readonly IUdpServerChannelFactory _udpServerServerChannelFactory;
        private readonly IPeerDiscovery _peerDiscovery;
        private readonly List<IP2PMessageObserver> _p2PMessageHandlers;
        private readonly EmbeddedObservableChannel _serverChannel;
        private IPeerService _peerService;
        private IPeerSettings _peerSettings;

        public PeerServiceTests(ITestOutputHelper output) : base(output)
        {
            _pid = PeerIdentifierHelper.GetPeerIdentifier("im_a_key");
            _guid = CorrelationId.GenerateCorrelationId();
            _logger = Substitute.For<ILogger>();

            _serverChannel = new EmbeddedObservableChannel($"Server:{CurrentTestName}");
            _udpServerServerChannelFactory = Substitute.For<IUdpServerChannelFactory>();

            _peerSettings = Substitute.For<IPeerSettings>();
            _peerSettings.BindAddress.Returns(IPAddress.Parse("127.0.0.1"));
            _peerSettings.Port.Returns(1234);

            _udpServerServerChannelFactory.BuildChannel(Arg.Any<IEventLoopGroupFactory>(), _peerSettings.BindAddress, _peerSettings.Port).Returns(_serverChannel);

            _peerDiscovery = Substitute.For<IPeerDiscovery>();
            _p2PMessageHandlers = new List<IP2PMessageObserver>();
        }

        [Fact]
        public async Task Can_receive_incoming_ping_responses()
        {
            var messageObserver = new TestMessageObserver<PingResponse>(_logger);
            var protocolMessage = new PingResponse().ToProtocolMessage(_pid.PeerId, _guid);

            await InitialisePeerServiceAndSendMessage(messageObserver, protocolMessage).ConfigureAwait(false);

            messageObserver.SubstituteObserver.Received().OnNext(Arg.Any<PingResponse>());
        }

        [Fact]
        public async Task Can_receive_PingRequest()
        {
            var pingRequestHandler = new TestMessageObserver<PingRequest>(_logger);
            var request = new PingRequest().ToProtocolMessage(_pid.PeerId, _guid);

            await InitialisePeerServiceAndSendMessage(pingRequestHandler, request).ConfigureAwait(false);

            pingRequestHandler.SubstituteObserver.Received().OnNext(Arg.Any<PingRequest>());
        }

        [Fact]
        public async Task Can_receive_PeerNeighborsRequest()
        {
            var pingRequestHandler = new TestMessageObserver<PeerNeighborsRequest>(_logger);
            var request = new PeerNeighborsRequest().ToProtocolMessage(_pid.PeerId, _guid);

            await InitialisePeerServiceAndSendMessage(pingRequestHandler, request).ConfigureAwait(false);

            pingRequestHandler.SubstituteObserver.Received().OnNext(Arg.Any<PeerNeighborsRequest>());
        }

        [Fact]
        public async Task Can_receive_PeerNeighborsResponse()
        {
            var pingRequestHandler = new TestMessageObserver<PeerNeighborsResponse>(_logger);
            var neighbourIds = "abc".Select(i => PeerIdHelper.GetPeerId(i.ToString()));
            var responseContent = new PeerNeighborsResponse();
            responseContent.Peers.AddRange(neighbourIds);

            var response = responseContent.ToProtocolMessage(_pid.PeerId, _guid);

            await InitialisePeerServiceAndSendMessage(pingRequestHandler, response).ConfigureAwait(false);

            pingRequestHandler.SubstituteObserver.Received(1).OnNext(Arg.Any<PeerNeighborsResponse>());
            var call = pingRequestHandler.SubstituteObserver.ReceivedCalls().Single();
            ((PeerNeighborsResponse) call.GetArguments()[0]).Peers
               .Should().BeEquivalentTo(responseContent.Peers);
        }

        private async Task InitialisePeerServiceAndSendMessage(IP2PMessageObserver pingRequestHandler, ProtocolMessage message)
        {
            _p2PMessageHandlers.Add(pingRequestHandler);

            _peerService = new PeerService(new UdpServerEventLoopGroupFactory(new EventLoopGroupFactoryConfiguration()), 
                _udpServerServerChannelFactory,
                _peerDiscovery,
                _p2PMessageHandlers,
                _peerSettings,
                _logger,
                Substitute.For<IPeerHeartbeatChecker>());

            await _peerService.StartAsync();
            await _serverChannel.SimulateReceivingMessagesAsync(message).ConfigureAwait(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            
            _peerService?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
