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
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.UnitTests.Stub;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Transport
{
    public sealed class SocketBaseUnitTests
    {
        [Fact]
        public void SocketBase_Should_Dispose()
        {
            var channelFactory = Substitute.For<ITcpClientChannelFactory>();
            var logger = Substitute.For<ILogger>();
            var eventLoopGroupFactory = Substitute.For<IEventLoopGroupFactory>();
            var testSocketBase = new TestSocketBase(channelFactory, logger, eventLoopGroupFactory);
            testSocketBase.Dispose();

            logger.Received(1).Debug($"Disposing{typeof(TestSocketBase).Name}");
        }

        [Fact]
        public void SocketBase_Should_Log_On_Dispose_Exception()
        {
            var channelFactory = Substitute.For<ITcpClientChannelFactory>();
            var logger = Substitute.For<ILogger>();
            var eventLoopGroupFactory = Substitute.For<IEventLoopGroupFactory>();
            var testSocketBase = new TestSocketBase(channelFactory, logger, eventLoopGroupFactory);
            var socketException = new SocketException();
            testSocketBase.Channel.CloseAsync().Throws(socketException);
            testSocketBase.DisposeProxy(true);

            logger.Received(1).Error(socketException, "Dispose failed to complete.");
        }

        [Fact]
        public void SocketBase_Should_Not_Dispose()
        {
            var channelFactory = Substitute.For<ITcpClientChannelFactory>();
            var logger = Substitute.For<ILogger>();
            var eventLoopGroupFactory = Substitute.For<IEventLoopGroupFactory>();
            var testSocketBase = new TestSocketBase(channelFactory, logger, eventLoopGroupFactory);
            testSocketBase.DisposeProxy(false);

            logger.DidNotReceive().Debug($"Disposing{typeof(TestSocketBase).Name}");
        }
    }
}
