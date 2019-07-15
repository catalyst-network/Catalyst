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

using Autofac;
using Catalyst.Common.Interfaces.Cli;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Cli.IntegrationTests.Commands
{
    public sealed class ConnectNodeTest : CliCommandTestBase
    {
        public ConnectNodeTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Cli_Can_Connect_To_Node()
        {
            using (var container = ContainerBuilder.Build())
            {
                using (container.BeginLifetimeScope(CurrentTestName))
                {
                    var shell = container.Resolve<ICatalystCli>();
                    var hasConnected = shell.ParseCommand("connect", "-n", "node1");
                    hasConnected.Should().BeTrue();
                }
            }
        }

        [Fact]
        public void Cli_Can_Handle_Multiple_Connection_Attempts()
        {
            using (var container = ContainerBuilder.Build())
            {
                using (container.BeginLifetimeScope(CurrentTestName))
                {
                    var shell = container.Resolve<ICatalystCli>();
                    for (var i = 0; i < 10; i++)
                    {
                        var canConnect = shell.ParseCommand("connect", "-n", "node1");
                        canConnect.Should().BeTrue();

                        var disconnected = shell.ParseCommand("disconnect", "-n", "node1");
                        disconnected.Should().BeTrue();
                    }
                }
            }
        }
    }
}
