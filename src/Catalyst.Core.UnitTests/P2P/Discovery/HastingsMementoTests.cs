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

using Catalyst.Abstractions.P2P;
using Catalyst.Core.Config;
using Catalyst.Core.P2P.Discovery.Hastings;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P.Discovery
{
    public sealed class HastingsMementoTests
    {
        private readonly IPeerIdentifier _peer;

        public HastingsMementoTests()
        {
            _peer = PeerIdentifierHelper.GetPeerIdentifier("current_peer");
        }
        
        [Fact]
        public void Can_Init_Memento_With_Existing_Params()
        {
            var neighbours = DiscoveryHelper.MockNeighbours();
            var memento = new HastingsMemento(_peer, neighbours);
            
            memento.Peer.Should().Be(_peer);
            memento.Neighbours.Should().Contain(neighbours);
            memento.Neighbours.Should().HaveCount(Constants.AngryPirate);
        }
    }
}
