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

using System.Net.Sockets;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.TestUtils;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Transport
{
    public sealed class SocketBaseUnitTests
    {
        public SocketBaseUnitTests()
        {
            _logger = Substitute.For<ILogger>();
            var channelFactory = Substitute.For<ITcpClientChannelFactory>();
            var eventLoopGroupFactory = Substitute.For<IEventLoopGroupFactory>();
            _testSocketBase = new TestSocketBase(channelFactory, _logger, eventLoopGroupFactory);
        }

        private readonly ILogger _logger;
        private readonly TestSocketBase _testSocketBase;

        [Fact]
        public void SocketBase_Should_Dispose()
        {
            _testSocketBase.Dispose();

            _logger.Received(1).Debug($"Disposing{typeof(TestSocketBase).Name}");
        }

        [Fact]
        public void SocketBase_Should_Log_On_Dispose_Exception()
        {
            var socketException = new SocketException();
            _testSocketBase.Channel.CloseAsync().Throws(socketException);
            _testSocketBase.DisposeProxy(true);

            _logger.Received(1).Error(socketException, "Dispose failed to complete.");
        }

        [Fact]
        public void SocketBase_Should_Not_Dispose()
        {
            _testSocketBase.DisposeProxy(false);

            _logger.DidNotReceive().Debug($"Disposing{typeof(TestSocketBase).Name}");
        }
    }
}
