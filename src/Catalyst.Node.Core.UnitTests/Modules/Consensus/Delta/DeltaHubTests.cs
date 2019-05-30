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

using Catalyst.Common.Interfaces.IO.Messaging.Gossip;
using Catalyst.Common.Interfaces.Modules.Consensus.Delta;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Consensus.Delta;
using Catalyst.Protocol.Common;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus.Delta
{
    public class DeltaHubTests
    {
        private readonly IGossipManager _gossipManager;
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly IDeltaVoter _deltaVoter;

        public DeltaHubTests()
        {
            _gossipManager = Substitute.For<IGossipManager>();
            _logger = Substitute.For<ILogger>();
            _peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("me");
            _deltaVoter = Substitute.For<IDeltaVoter>();
        }

        [Fact]
        public void BroadcastCandidate_should_only_gossip_round_nodes_own_candidate()
        {
            var hub = new DeltaHub(_gossipManager, _peerIdentifier, _deltaVoter, _logger);

            var notMyCandidate = CandidateDeltaHelper.GetCandidateDelta(
                producerId: PeerIdHelper.GetPeerId("not me"));

            hub.BroadcastCandidate(notMyCandidate);
            _gossipManager.Received(0).Broadcast(null);

            var myCandidate = CandidateDeltaHelper.GetCandidateDelta(
                producerId: _peerIdentifier.PeerId);

            hub.BroadcastCandidate(myCandidate);
            _gossipManager.Received(1).Broadcast(null);
        }

        [Fact]
        public void BroadcastFavoriteCandidateDelta()
        {

        }
    }
}
