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
using System.Linq;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P.Discovery
{
    public sealed class HastingCareTakerTests
    {
        [Fact]
        public void Care_Taker_Can_Enqueue_State()
        {
            var careTaker = new HastingCareTaker();

            var subbedMemento = Substitute.For<IHastingMemento>();
            
            careTaker.Add(subbedMemento);

            careTaker.HastingMementoList.Contains(subbedMemento);
            careTaker.HastingMementoList.Should().HaveCount(1);
        }
        
        [Fact]
        public void Care_Taker_Can_Dequeue_State()
        {
            var careTaker = new HastingCareTaker();

            var subbedMemento = Substitute.For<IHastingMemento>();
            
            careTaker.Add(subbedMemento);
            careTaker.Add(subbedMemento);
            careTaker.HastingMementoList.Should().HaveCount(2);
            var previousState = careTaker.Get();
            previousState.Should().BeSameAs(subbedMemento);
            careTaker.HastingMementoList.Should().HaveCount(1);
        }
        
        [Fact]
        public void Care_Taker_Should_Throw_Exception_Dequeuing_Empty_Queue()
        {
            var careTaker = new HastingCareTaker();
            
            Assert.Throws<Exception>(() =>
            {
                var previousState = careTaker.Get();
            });
        }
        
        [Fact]
        public void Can_Never_Take_Last_Memento()
        {
            var careTaker = new HastingCareTaker();

            var subbedMemento = Substitute.For<IHastingMemento>();
            
            careTaker.Add(subbedMemento);
            
            careTaker.Get();

            careTaker.HastingMementoList.Count.Should().Be(1);
        }

        [Fact]
        public void Can_LIFO_When_History_N_Plus2()
        {
            var careTaker = new HastingCareTaker();

            var subbedMemento1 = Substitute.For<IHastingMemento>();
            subbedMemento1.Peer.Returns(PeerIdentifierHelper.GetPeerIdentifier("step1"));
            careTaker.Add(subbedMemento1);

            var subbedMemento2 = Substitute.For<IHastingMemento>();
            subbedMemento2.Peer.Returns(PeerIdentifierHelper.GetPeerIdentifier("step2"));
            careTaker.Add(subbedMemento2);
            
            var subbedMemento3 = Substitute.For<IHastingMemento>();
            subbedMemento3.Peer.Returns(PeerIdentifierHelper.GetPeerIdentifier("step3"));
            careTaker.Add(subbedMemento3);
            
            var lastState1 = careTaker.Get();
            lastState1.Should().Be(subbedMemento3);
            careTaker.HastingMementoList.Count.Should().Be(2);
            careTaker.HastingMementoList.First().Should().Be(subbedMemento2);
            
            var lastState2 = careTaker.Get();
            lastState2.Should().Be(subbedMemento2);
            careTaker.HastingMementoList.Count.Should().Be(1);
            careTaker.HastingMementoList.First().Should().Be(subbedMemento1);
        }
    }
}
