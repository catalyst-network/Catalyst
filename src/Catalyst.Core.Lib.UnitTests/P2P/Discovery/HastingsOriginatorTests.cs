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

using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P.Discovery
{
    public sealed class HastingsOriginatorTests
    {
        private readonly IPeerIdentifier _peer;

        public HastingsOriginatorTests()
        {
            _peer = PeerIdentifierHelper.GetPeerIdentifier("current_peer");
        }

        [Fact]
        public void Can_Create_Memento_From_Current_State()
        {
            var originator = new HastingsOriginator();

            var state = new List<IPeerIdentifier>
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
            var memento = DiscoveryHelper.MockMemento();
            var originator = new HastingsOriginator();
            
            originator.RestoreMemento(memento);

            originator.Peer
               .Should()
               .Be(memento.Peer);
            
            originator.CurrentPeersNeighbours.Count
               .Should()
               .Be(0);
            
            originator.UnResponsivePeers.Select(i => i.Key)
               .ToList()
               .Should()
               .BeSubsetOf(memento.Neighbours);
        }

        [Fact]
        public void Can_Increment_UnReachable_Peer()
        {
            var originator = new HastingsOriginator();
            
            Enumerable.Range(0, 100)
               .ToList()
               .ForEach(i =>
                {
                    originator.UnResponsivePeers.Add(
                        new KeyValuePair<IPeerIdentifier, ICorrelationId>(Substitute.For<IPeerIdentifier>(), Substitute.For<ICorrelationId>())
                    );
                });

            originator.UnResponsivePeers.Count.Should().Be(100);
        }

        [Fact]
        public void Can_Clean_Up_When_Calling_SetMemento()
        {
            var originator = new HastingsOriginator();

            var memento1 = DiscoveryHelper.SubMemento();
            var memento2 = DiscoveryHelper.SubMemento();
            
            originator.RestoreMemento(memento1);
            originator.CurrentPeersNeighbours = DiscoveryHelper.MockNeighbours().ToList();
            originator.UnResponsivePeers = DiscoveryHelper.MockUnResponsiveNeighbours();
            originator.ExpectedPnr = DiscoveryHelper.MockPnr();

            originator.RestoreMemento(memento2);
            
            originator.UnResponsivePeers.Select(i => i.Key).Should()
               .BeSubsetOf(memento2.Neighbours);

            originator.UnResponsivePeers.Count.Should().Be(Constants.AngryPirate);
            originator.ExpectedPnr.Key.Should().Be(null);
            originator.ExpectedPnr.Value.Should().Be(null);
            originator.CurrentPeersNeighbours.Should().BeSubsetOf(memento2.Neighbours);
        }
        
        [Fact]
        public void Can_Clean_Up_When_Setting_Peer()
        {
            var originator = new HastingsOriginator();

            var memento1 = DiscoveryHelper.SubMemento();
            
            originator.RestoreMemento(memento1);
            originator.CurrentPeersNeighbours = DiscoveryHelper.MockNeighbours().ToList();
            originator.UnResponsivePeers = DiscoveryHelper.MockContactedNeighboursValuePairs(DiscoveryHelper.MockNeighbours());
            originator.ExpectedPnr = DiscoveryHelper.MockPnr();

            originator.Peer = PeerIdentifierHelper.GetPeerIdentifier("new_peer");

            originator.UnResponsivePeers.Count.Should().Be(0);
            originator.ExpectedPnr.Key.Should().Be(null);
            originator.ExpectedPnr.Value.Should().Be(null);
        }
    }
}
