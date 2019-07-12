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
using System;
using Catalyst.Cli.IntegrationTests.Commands;

namespace Catalyst.Cli.IntegrationTests.Connection
{
    public sealed class CliToNodeTests : CliCommandTestBase, IDisposable
    {
        private readonly TestCatalystNode _node;

        public CliToNodeTests(ITestOutputHelper output) : base(output, false)
        {
            _node = new TestCatalystNode("server", output);
        }

        [Fact]
        public void CliToNode_Connect_To_Node()
        {
            _node.RunNodeInstance();

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

        public new void Dispose()
        {
            base.Dispose();
            _node?.Dispose();
        }
    }
}
