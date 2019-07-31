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

using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.P2P;
using Catalyst.Core.Lib.P2P;
using Catalyst.TestUtils;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P
{
    public sealed class PeerHeartbeatCheckerTests : IDisposable
    {
        private readonly int _peerHeartbeatCheckSeconds = 5;
        private IPeerHeartbeatChecker _peerHeartbeatChecker;
        private readonly IPeerService _peerService;
        private readonly IPeerClient _peerClient;
        private readonly IPeerIdentifier _senderIdentifier;
        private readonly IPeerRepository _peerRepository;
        private readonly Peer _testPeer;

        public PeerHeartbeatCheckerTests()
        {
            _peerRepository = Substitute.For<IPeerRepository>();
            _senderIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Sender");
            _peerClient = Substitute.For<IPeerClient>();
            _peerService = Substitute.For<IPeerService>();
            _testPeer = new Peer
            {
                PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("TestPeer")
            };
        }

        [Fact]
        public async Task Can_Remove_Peer_On_Non_Responsive_Heartbeat()
        {
            await RunHeartbeatChecker();
            _peerRepository.Received(1).Delete(_testPeer.DocumentId);
        }

        [Fact]
        public async Task Can_Keep_Peer_On_Valid_Heartbeat_Response()
        {
            var heartbeatReply = new PingResponse().ToProtocolMessage(_testPeer.PeerIdentifier.PeerId,
                CorrelationId.GenerateCorrelationId());
            var messageStream =
                MessageStreamHelper.CreateStreamWithMessage(Substitute.For<IChannelHandlerContext>(), heartbeatReply);
            _peerService.MessageStream.Returns(messageStream);

            await RunHeartbeatChecker();
            _peerRepository.DidNotReceive().Delete(_testPeer.DocumentId);
        }

        private async Task RunHeartbeatChecker()
        {
            var peers = new List<Peer> {_testPeer};

            _peerRepository.GetAll().Returns(peers);
            _peerRepository.AsQueryable().Returns(peers.AsQueryable());
            _peerHeartbeatChecker = new PeerHeartbeatChecker(_peerRepository,
                new PeerChallenger(Substitute.For<ILogger>(), _peerClient, _senderIdentifier, _peerHeartbeatCheckSeconds),
                _peerHeartbeatCheckSeconds);

            _peerHeartbeatChecker.Run();
            await Task.Delay(TimeSpan.FromSeconds(_peerHeartbeatCheckSeconds / 2D)).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _peerHeartbeatChecker?.Dispose();
        }
    }
}
