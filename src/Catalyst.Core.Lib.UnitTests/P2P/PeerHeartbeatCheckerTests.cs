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
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P
{
    public class PeerHeartbeatCheckerTests
    {
        private readonly IPeerHeartbeatChecker _peerHeatbeatChecker;
        private readonly IPeerService _peerService;
        private readonly IPeerClient _peerClient;
        private readonly IPeerIdentifier _senderIdentifier;
        private readonly TimeSpan _peerHeartbeatCheckTimeSpan;
        private readonly IPeerRepository _peerRepository;
        private readonly Peer _testPeer;

        public PeerHeartbeatCheckerTests()
        {
            _peerHeartbeatCheckTimeSpan = TimeSpan.FromSeconds(5);

            var peers = new List<Peer>();

            _peerRepository = Substitute.For<IPeerRepository>();
            _senderIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Sender");
            _peerClient = Substitute.For<IPeerClient>();
            _peerService = Substitute.For<IPeerService>();
            _testPeer = new Peer
            {
                PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("TestPeer")
            };

            peers.Add(_testPeer);
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
            /*_peerRepository.GetAll().Returns(peers);
            _peerRepository.AsQueryable().Returns(peers.AsQueryable());
            _peerHeatbeatChecker = new PeerHeartbeatChecker(_peerRepository,
                new PeerChallenger(_peerService, logger, _peerClient, _senderIdentifier),
                _peerHeartbeatCheckTimeSpan);

            _peerHeatbeatChecker.Run();
            await Task.Delay(_peerHeartbeatCheckTimeSpan).ConfigureAwait(false);*/
        }
    }
}
