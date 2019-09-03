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
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Transport
{
    public sealed class TcpClientUnitTests
    {
        public TcpClientUnitTests()
        {
            _tcpClientChannelFactory = Substitute.For<ITcpClientChannelFactory>();
            _logger = Substitute.For<ILogger>();
            _eventLoopGroupFactory = Substitute.For<ITcpClientEventLoopGroupFactory>();
        }

        private readonly ITcpClientChannelFactory _tcpClientChannelFactory;
        private readonly ILogger _logger;
        private readonly ITcpClientEventLoopGroupFactory _eventLoopGroupFactory;

        [Fact]
        public async Task TcpClient_Should_Be_Derivable()
        {
            var tcpClient = new TestTcpClient(_tcpClientChannelFactory, _logger, _eventLoopGroupFactory);
            var exception = await Record.ExceptionAsync(() => tcpClient.StartAsync());
            exception.Should().BeOfType<NotImplementedException>();
        }
    }
}
