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
using Catalyst.Node.Core.P2P.Discovery;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P.Discovery
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
            var previousState = careTaker.Get();

            previousState.Should().BeSameAs(subbedMemento);
            careTaker.HastingMementoList.Should().HaveCount(0);
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
    }
}
