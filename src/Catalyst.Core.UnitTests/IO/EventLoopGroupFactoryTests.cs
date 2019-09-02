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
using System.Reflection;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Core.IO.EventLoop;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.IO
{
    public sealed class EventLoopGroupFactoryTests : IDisposable
    {
        private const int ExpectedUdpServerThreads = 2;
        private const int ExpectedTcpServerThreads = 3;
        private const int ExpectedUdpClientThreads = 4;
        private const int ExpectedTcpClientThreads = 5;
        private static readonly int ExpectedDefaultEventLoopThreadCount = Environment.ProcessorCount * 2;
        private IEventLoopGroupFactory _eventFactory;

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
            _eventFactory = new UdpServerEventLoopGroupFactory(_eventLoopGroupFactoryConfiguration);
            AssertEventLoopSize(_eventFactory.GetOrCreateHandlerWorkerEventLoopGroup(), ExpectedUdpServerThreads);
            AssertEventLoopSize(_eventFactory.GetOrCreateSocketIoEventLoopGroup(), ExpectedDefaultEventLoopThreadCount);
        }

        [Fact]
        public void Can_Spawn_Correct_Amount_Of_Udp_Client_Event_Loops()
        {
            _eventFactory = new UdpClientEventLoopGroupFactory(_eventLoopGroupFactoryConfiguration);
            AssertEventLoopSize(_eventFactory.GetOrCreateHandlerWorkerEventLoopGroup(), ExpectedUdpClientThreads);
            AssertEventLoopSize(_eventFactory.GetOrCreateSocketIoEventLoopGroup(), ExpectedDefaultEventLoopThreadCount);
        }

        [Fact]
        public void Can_Spawn_Correct_Amount_Of_Tcp_Server_Event_Loops()
        {
            _eventFactory = new TcpServerEventLoopGroupFactory(_eventLoopGroupFactoryConfiguration);
            AssertEventLoopSize(_eventFactory.GetOrCreateHandlerWorkerEventLoopGroup(), ExpectedTcpServerThreads);
            AssertEventLoopSize(_eventFactory.GetOrCreateSocketIoEventLoopGroup(), ExpectedDefaultEventLoopThreadCount);
        }

        [Fact]
        public void Can_Spawn_Correct_Amount_Of_Tcp_Client_Event_Loops()
        {
            _eventFactory = new TcpClientEventLoopGroupFactory(_eventLoopGroupFactoryConfiguration);
            AssertEventLoopSize(_eventFactory.GetOrCreateHandlerWorkerEventLoopGroup(), ExpectedTcpClientThreads);
            AssertEventLoopSize(_eventFactory.GetOrCreateSocketIoEventLoopGroup(), ExpectedDefaultEventLoopThreadCount);
        }

        [Fact]
        public async Task Can_Dispose_All_Event_Loops()
        {
            _eventFactory = new TcpClientEventLoopGroupFactory(_eventLoopGroupFactoryConfiguration);
            IEventLoopGroup[] eventLoops =
            {
                _eventFactory.GetOrCreateHandlerWorkerEventLoopGroup(),
                _eventFactory.GetOrCreateSocketIoEventLoopGroup()
            };

            _eventFactory.Dispose();

            while (!eventLoops.All(x => x.IsShutdown))
            {
                await Task.Delay(100).ConfigureAwait(false);
            }

            eventLoops.ToList().ForEach(eventLoop => eventLoop.IsShutdown.Should().BeTrue());
        }

        private void AssertEventLoopSize(IEventLoopGroup eventLoopGroup, int expectedEventLoops)
        {
            eventLoopGroup.Should().NotBeNull();
            var eventLoops =
                (IEventLoop[]) eventLoopGroup.GetType().GetField("eventLoops",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(eventLoopGroup);
            eventLoops.Length.Should().Be(expectedEventLoops);
        }

        public void Dispose() { _eventFactory?.Dispose(); }
    }
}
