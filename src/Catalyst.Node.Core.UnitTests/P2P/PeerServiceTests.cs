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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.IO.Transport;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observables;
using Catalyst.Node.Core.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.P2P
{
    public sealed class PeerServiceTests : SelfAwareTestBase, IDisposable
    {
        private readonly Guid _guid;
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _pid;
        private readonly IUdpServerChannelFactory _udpServerServerChannelFactory;
        private readonly IPeerDiscovery _peerDiscovery;
        private readonly List<IP2PMessageObserver> _p2PMessageHandlers;
        private readonly EmbeddedObservableChannel _serverChannel;
        private PeerService _peerService;

        public PeerServiceTests(ITestOutputHelper output) : base(output)
        {
            _pid = PeerIdentifierHelper.GetPeerIdentifier("im_a_key");
            _guid = Guid.NewGuid();
            _logger = Substitute.For<ILogger>();

            _serverChannel = new EmbeddedObservableChannel($"Server:{CurrentTestName}");
            _udpServerServerChannelFactory = Substitute.For<IUdpServerChannelFactory>();
            _udpServerServerChannelFactory.BuildChannel().Returns(_serverChannel);

            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.BindAddress.Returns(IPAddress.Parse("127.0.0.1"));
            peerSettings.Port.Returns(1234);

            _peerDiscovery = Substitute.For<IPeerDiscovery>();
            _p2PMessageHandlers = new List<IP2PMessageObserver>();
        }

        [Fact]
        public async Task Can_receive_incoming_ping_responses()
        {
            var pingHandler = new TestMessageObserver<PingResponse>(_logger);
            var protocolMessage = new PingResponse().ToProtocolMessage(_pid.PeerId, _guid);

            await InitialisePeerServiceAndSendMessage(pingHandler, protocolMessage).ConfigureAwait(false);

            pingHandler.SubstituteObserver.Received().OnNext(Arg.Any<PingResponse>());
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
            ((PeerNeighborsResponse)call.GetArguments()[0]).Peers
               .Should().BeEquivalentTo(responseContent.Peers);
        }

        private async Task InitialisePeerServiceAndSendMessage(IP2PMessageObserver pingRequestHandler, ProtocolMessage message)
        {
            _p2PMessageHandlers.Add(pingRequestHandler);

            _peerService = new PeerService(_udpServerServerChannelFactory,
                _peerDiscovery,
                new HandlerWorkerEventLoopGroupFactory(1, 1, 1, 1),
                _p2PMessageHandlers,
                _logger);

            await _serverChannel.SimulateReceivingMessages(message).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _peerService?.Dispose();
        }
    }
}
