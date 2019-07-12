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
using System.Linq;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Node.Core.P2P.Discovery;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P.Discovery
{
    public sealed class HastingsOriginatorTests
    {
        private readonly IPeerIdentifier _peer;

        public HastingsOriginatorTests()
        {
            _peer = PeerIdentifierHelper.GetPeerIdentifier("current_peer");
        }
        
        private IHastingMemento BuildMemento()
        {
            var neighbours = Enumerable.Range(0, 5).Select(i =>
                PeerIdentifierHelper.GetPeerIdentifier($"neighbour-{i.ToString()}")).ToList();
            
            var memento = new HastingMemento(_peer, neighbours);

            return memento;
        }

        [Fact]
        public void Can_Create_Memento_From_Current_State()
        {
            var originator = new HastingsOriginator();

            var state = new ConcurrentBag<IPeerIdentifier>
            {
                PeerIdentifierHelper.GetPeerIdentifier("peer-1")
            };
            
            originator.Peer = _peer;
            originator.CurrentPeersNeighbours = state;

            var stateMemento = originator.CreateMemento();

            stateMemento.Neighbours.Should().Contain(state);
            stateMemento.Peer.Should().Be(_peer);
        }

        [Fact]
        public void Can_Restore_State_From_Memento()
        {
            var memento = BuildMemento();
            var originator = new HastingsOriginator();
            
            originator.SetMemento(memento);

            originator.Peer.Should().Be(_peer);
            originator.CurrentPeersNeighbours.Should().Contain(memento.Neighbours);
        }
    }
}
