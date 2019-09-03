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
using System.Net;
using System.Threading.Tasks;
using Catalyst.Core.IO.Transport.Bootstrapping;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Transport.Bootstrapping
{
    public sealed class BootstrapUnitTests
    {
        [Fact]
        public async Task BindAsync_Should_Bind_To_NettyBootstrap_BindAsync()
        {
            var ipAddress = IPAddress.Loopback;
            var port = 9000;

            var bootstrap = new Bootstrap();

            //We have not set the group for bootstrap so we know that code will trigger an exception, if BindAsync calls Base BindAsync
            var exception = await Record.ExceptionAsync(async () => { await bootstrap.BindAsync(ipAddress, port); });
            exception.Should().BeOfType<InvalidOperationException>("group not set");
        }
    }
}
