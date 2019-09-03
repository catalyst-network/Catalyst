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
using System.Collections.Generic;
using Catalyst.Abstractions.Cli.CommandTypes;
using Catalyst.Abstractions.Rpc;
using Catalyst.Cli.Commands;
using Catalyst.Cli.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Cli.UnitTests.Commands
{
    public sealed class ConnectTests
    {
        private readonly TestScheduler _testScheduler = new TestScheduler();

        [Fact]
        public void Cannot_Connect_With_Invalid_Config()
        {
            var commandContext = TestCommandHelpers.GenerateCliCommandContext();
            commandContext.GetNodeConfig(Arg.Any<string>()).Returns((IRpcNodeConfig) null);

            var commands = new List<ICommand> {new ConnectCommand(commandContext)};
            var console = new CatalystCli(commandContext.UserOutput, commands);

            var exception = Record.Exception(() => console.ParseCommand("connect", "-n", "node1"));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Cannot_Connect_With_Invalid_SocketChannel()
        {
            var commandContext = TestCommandHelpers.GenerateCliCommandContext();
            TestCommandHelpers.MockRpcNodeConfig(commandContext);

            var commands = new List<ICommand> {new ConnectCommand(commandContext)};
            var console = new CatalystCli(commandContext.UserOutput, commands);

            var isCommandParsed = console.ParseCommand("connect", "-n", "test");
            isCommandParsed.Should().BeFalse();

            commandContext.UserOutput.Received(1).WriteLine(ConnectCommand.InvalidSocketChannel);
        }

        [Fact]
        public void Connect_Should_Connect_To_Node()
        {
            var commandContext = TestCommandHelpers.GenerateCliFullCommandContext();
            TestCommandHelpers.AddClientSocketRegistry(commandContext, _testScheduler);

            var commands = new List<ICommand> {new ConnectCommand(commandContext)};
            var console = new CatalystCli(commandContext.UserOutput, commands);

            var isCommandParsed = console.ParseCommand("connect", "-n", "test");
            isCommandParsed.Should().BeTrue();

            commandContext.SocketClientRegistry.Registry.Count.Should().Be(1);
            commandContext.UserOutput.Received(1)
               .WriteLine($"Connected to Node {commandContext.GetConnectedNode("test").Channel.RemoteAddress}");
        }
    }
}
