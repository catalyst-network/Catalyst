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

using System.Net;
using System.Threading.Tasks;
using Catalyst.Core.IO.EventLoop;
using Catalyst.Core.IO.Transport.Channels;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Transport.Channels
{
    public sealed class TcpClientChannelFactoryTests
    {
        [Fact]
        public async Task TcpClientChannelFactory_BuildChannel_Should_Return_IObservableChannel()
        {
            var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
            {
                TcpClientHandlerWorkerThreads = 2
            };

            var eventLoopGroupFactory = new TcpClientEventLoopGroupFactory(eventLoopGroupFactoryConfiguration);
            var ipAddress = IPAddress.Loopback;
            var port = 9000;

            var testTcpClientChannelFactory = new TestTcpClientChannelFactory();
            var channel = await testTcpClientChannelFactory.BuildChannel(eventLoopGroupFactory, ipAddress, port);

            channel.Should().BeOfType<ObservableChannel>();
        }
    }
}
