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

using System.Diagnostics;
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.UnitTests.Stub;
using DotNetty.Transport.Channels;
using NSubstitute;
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

            //var isChannelActive = testSocketBase.Channel.Active;

            //testSocketBase.Dispose();
        }
    }
}
