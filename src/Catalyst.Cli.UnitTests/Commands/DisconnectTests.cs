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

using System.Collections.Generic;
using Catalyst.Abstractions.Cli.CommandTypes;
using Catalyst.Cli.Commands;
using Catalyst.Cli.UnitTests.Helpers;
using Catalyst.Core.Network;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Xunit;

namespace Catalyst.Cli.UnitTests.Commands
{
    public sealed class DisconnectTests
    {
        private readonly TestScheduler _testScheduler = new TestScheduler();

        [Fact]
        public void Disconnect_Should_Disconnect_From_Node()
        {
            var commandContext = TestCommandHelpers.GenerateCliCommandContext();

            var nodeRpcClient = TestCommandHelpers.MockNodeRpcClient();
            TestCommandHelpers.MockActiveConnection(commandContext, nodeRpcClient);
            TestCommandHelpers.MockNodeRpcClientFactory(commandContext, nodeRpcClient);
            var rpcNodeConfig = TestCommandHelpers.MockRpcNodeConfig(commandContext);
            var socketClientRegistry = TestCommandHelpers.AddClientSocketRegistry(commandContext, _testScheduler);

            var clientHashCode =
                socketClientRegistry.GenerateClientHashCode(
                    EndpointBuilder.BuildNewEndPoint(rpcNodeConfig.HostAddress, rpcNodeConfig.Port));
            socketClientRegistry.AddClientToRegistry(clientHashCode, nodeRpcClient);

            var commands = new List<ICommand> {new DisconnectCommand(commandContext)};
            var console = new CatalystCli(commandContext.UserOutput, commands);

            var isCommandParsed = console.ParseCommand("disconnect", "-n", "node1");
            isCommandParsed.Should().BeTrue();

            socketClientRegistry.Registry.Count.Should().Be(0);
        }
    }
}
