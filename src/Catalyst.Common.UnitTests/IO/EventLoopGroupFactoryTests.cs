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
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.IO.EventLoop;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Xunit;

namespace Catalyst.Common.UnitTests.IO
{
    public class EventLoopGroupFactoryTests
    {
        private readonly int ExpectedUdpServerThreads = 2;
        private readonly int ExpectedTcpServerThreads = 3;
        private readonly int ExpectedUdpClientThreads = 4;
        private readonly int ExpectedTcpClientThreads = 5;

        private readonly IEventLoopGroupFactoryConfiguration _eventLoopGroupFactoryConfiguration;
        
        public EventLoopGroupFactoryTests()
        {
            _eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
            {
                TcpClientHandlerWorkerThreads = ExpectedTcpClientThreads,
                TcpServerHandlerWorkerThreads = ExpectedTcpServerThreads,
                UdpServerHandlerWorkerThreads = ExpectedUdpServerThreads,
                UdpClientHandlerWorkerThreads = ExpectedUdpClientThreads
            };
        }

        [Fact]
        public void Can_Spawn_Correct_Amount_Of_Udp_Server_Event_Loops()
        {
            IEventLoopGroupFactory eventFactory = new UdpServerEventLoopGroupFactory(_eventLoopGroupFactoryConfiguration);
            AssertEventLoopSize(eventFactory, ExpectedUdpServerThreads);
        }

        [Fact]
        public void Can_Spawn_Correct_Amount_Of_Udp_Client_Event_Loops()
        {
            IEventLoopGroupFactory eventFactory = new UdpClientEventLoopGroupFactory(_eventLoopGroupFactoryConfiguration);
            AssertEventLoopSize(eventFactory, ExpectedUdpClientThreads);
        }

        [Fact]
        public void Can_Spawn_Correct_Amount_Of_Tcp_Server_Event_Loops()
        {
            IEventLoopGroupFactory eventFactory = new TcpServerEventLoopGroupFactory(_eventLoopGroupFactoryConfiguration);
            AssertEventLoopSize(eventFactory, ExpectedTcpServerThreads);
        }

        [Fact]
        public void Can_Spawn_Correct_Amount_Of_Tcp_Client_Event_Loops()
        {
            IEventLoopGroupFactory eventFactory = new TcpClientEventLoopGroupFactory(_eventLoopGroupFactoryConfiguration);
            AssertEventLoopSize(eventFactory, ExpectedTcpClientThreads);
        }

        private void AssertEventLoopSize(IEventLoopGroupFactory eventLoopGroupFactory, int expectedEventLoops)
        {
            IEventLoopGroup eventLoopGroup = eventLoopGroupFactory.GetOrCreateHandlerWorkerEventLoopGroup();
            eventLoopGroup.Should().NotBeNull();
            IEventLoop[] eventLoops =
                (IEventLoop[]) eventLoopGroup.GetType().GetField("eventLoops",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(eventLoopGroup);
            eventLoops.Length.Should().Be(expectedEventLoops);
            eventLoopGroup.ShutdownGracefullyAsync();
        }
    }
}
