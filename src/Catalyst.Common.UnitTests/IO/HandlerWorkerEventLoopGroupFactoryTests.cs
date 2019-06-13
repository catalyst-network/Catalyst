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

using System.Reflection;
using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.IO;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Xunit;

namespace Catalyst.Common.UnitTests.IO
{
    public class HandlerWorkerEventLoopGroupFactoryTests
    {
        private readonly int ExpectedUdpServerThreads = 2;
        private readonly int ExpectedTcpServerThreads = 3;
        private readonly int ExpectedUdpClientThreads = 4;
        private readonly int ExpectedTcpClientThreads = 5;
        private readonly IHandlerWorkerEventLoopGroupFactory _handlerWorkerEventLoopGroupFactory;

        public HandlerWorkerEventLoopGroupFactoryTests()
        {
            _handlerWorkerEventLoopGroupFactory =
                new HandlerWorkerEventLoopGroupFactory(ExpectedTcpClientThreads,
                    ExpectedTcpServerThreads,
                    ExpectedUdpServerThreads,
                    ExpectedUdpClientThreads);
        }

        [Fact]
        public void CanSpawnCorrectAmountOfUdpServerEventLoops()
        {
            AssertEventLoopSize(_handlerWorkerEventLoopGroupFactory.NewUdpServerLoopGroup(), ExpectedUdpServerThreads);
        }

        [Fact]
        public void CanSpawnCorrectAmountOfUdpClientEventLoops()
        {
            AssertEventLoopSize(_handlerWorkerEventLoopGroupFactory.NewUdpClientLoopGroup(), ExpectedUdpClientThreads);
        }

        [Fact]
        public void CanSpawnCorrectAmountOTcpServerEventLoops()
        {
            AssertEventLoopSize(_handlerWorkerEventLoopGroupFactory.NewTcpServerLoopGroup(), ExpectedTcpServerThreads);
        }

        [Fact]
        public void CanSpawnCorrectAmountOTcpClientEventLoops()
        {
            AssertEventLoopSize(_handlerWorkerEventLoopGroupFactory.NewTcpClientLoopGroup(), ExpectedTcpClientThreads);
        }

        private void AssertEventLoopSize(IEventLoopGroup eventLoopGroup, int expectedEventLoops)
        {
            eventLoopGroup.Should().NotBeNull();
            IEventLoop[] eventLoops =
                (IEventLoop[])eventLoopGroup.GetType().GetField("eventLoops",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(eventLoopGroup);
            eventLoops.Length.Should().Be(expectedEventLoops);
        }
    }
}
