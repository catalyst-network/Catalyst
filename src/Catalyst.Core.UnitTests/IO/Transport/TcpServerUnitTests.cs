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

using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.TestUtils;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Transport
{
    public sealed class TcpServerUnitTests
    {
        public TcpServerUnitTests()
        {
            _tcpServerChannelFactory = Substitute.For<ITcpServerChannelFactory>();
            _logger = Substitute.For<ILogger>();
            _eventLoopGroupFactory = Substitute.For<IEventLoopGroupFactory>();
        }

        private readonly ITcpServerChannelFactory _tcpServerChannelFactory;
        private readonly ILogger _logger;
        private readonly IEventLoopGroupFactory _eventLoopGroupFactory;

        [Fact]
        public void TcpServer_Should_Dispose()
        {
            var tcpServer = new TestTcpServer(_tcpServerChannelFactory, _logger, _eventLoopGroupFactory);
            tcpServer.DisposeProxy(true);

            _logger.Received(1).Debug($"Disposing{typeof(TestTcpServer).Name}");
        }

        [Fact]
        public void TcpServer_Should_Not_Dispose()
        {
            var tcpServer = new TestTcpServer(_tcpServerChannelFactory, _logger, _eventLoopGroupFactory);
            tcpServer.DisposeProxy(false);

            _logger.DidNotReceive().Debug($"Disposing{typeof(TestTcpServer).Name}");
        }
    }
}
