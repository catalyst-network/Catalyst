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
using Catalyst.Core.P2P.Discovery.Hastings;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P.Discovery
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
            var memento = DiscoveryHelper.SubMemento(_peer);
            var originator = new HastingsOriginator(memento);

            var stateMemento = originator.CreateMemento();

            stateMemento.Peer.Should().Be(_peer);
            
            stateMemento.Neighbours
               .Should()
               .BeEquivalentTo(memento.Neighbours);
        }

        [Fact]
        public void Can_Restore_State_From_Memento_And_Assign_New_CorrelationId()
        {
            var memento = DiscoveryHelper.MockMemento();
            var originator = new HastingsOriginator(memento);
            
            originator.RestoreMemento(memento);

            originator.Peer.Should().Be(memento.Peer);
            originator.Neighbours.Should().BeEquivalentTo(memento.Neighbours);

            originator.PnrCorrelationId.Should().NotBe(default);
        }

        [Fact]
        public void Can_Clean_Up_When_Calling_RestoreMemento()
        {
            var originator = HastingsOriginator.Default;

            var memento1 = DiscoveryHelper.SubMemento();
            var memento2 = DiscoveryHelper.SubMemento();
            
            originator.RestoreMemento(memento1);
            originator.RestoreMemento(memento2);

            originator.PnrCorrelationId.Should().NotBe(default);
            
            originator.Peer.Should().BeEquivalentTo(memento2.Peer);
            originator.Neighbours.Should().BeEquivalentTo(memento2.Neighbours);
        }
    }
}
