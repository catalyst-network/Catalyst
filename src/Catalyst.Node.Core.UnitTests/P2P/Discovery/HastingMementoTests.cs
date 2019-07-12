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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Node.Core.P2P.Discovery;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P.Discovery
{
    public sealed class HastingMementoTests
    {
        private readonly IPeerIdentifier _peer;

        public HastingMementoTests() { _peer = PeerIdentifierHelper.GetPeerIdentifier("current_peer"); }
        
        private static List<IPeerIdentifier> GenerateNeighbours()
        {
            return Enumerable.Range(0, 5).Select(i =>
                PeerIdentifierHelper.GetPeerIdentifier($"neighbour-{i.ToString()}")).ToList();
        }
        
        [Fact]
        public void Can_Add_Peers_To_Memento_List()
        {
            var neighbours = GenerateNeighbours();

            var memento = new HastingMemento(_peer, neighbours);
            
            memento.Peer.Should().Be(_peer);
            memento.Neighbours.Should().Contain(neighbours);
            memento.Neighbours.Should().HaveCount(5);
        }

        [Fact]
        public void Can_Init_Memento_With_Existing_Params()
        {
            var neighbours = GenerateNeighbours();

            var memento = new HastingMemento(_peer, neighbours);

            memento.Peer.Should().Be(_peer);
            memento.Neighbours.Should().Contain(neighbours);
            memento.Neighbours.Should().HaveCount(5);
        }
    }
}
