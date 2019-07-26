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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Cli.Commands;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cli.CommandTypes;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Transport;
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
        public void Connect_Should_Connect_To_Node()
        {
            var logger = Substitute.For<ILogger>();
            var userOutput = Substitute.For<IUserOutput>();
            var nodeRpcFactory = Substitute.For<INodeRpcClientFactory>();
            var clientSocketRegistry = new SocketClientRegistry<INodeRpcClient>(_testScheduler);

            var nodeRpcClient = Substitute.For<INodeRpcClient>();
            nodeRpcClient.Channel.Active.Returns(true);

            var commandContext = Substitute.For<ICommandContext>();
            nodeRpcFactory.GetClient(Arg.Any<X509Certificate2>(), Arg.Any<IRpcNodeConfig>()).Returns(nodeRpcClient);
            commandContext.IsSocketChannelActive(Arg.Any<INodeRpcClient>()).Returns(true);
            commandContext.SocketClientRegistry.Returns(clientSocketRegistry);
            commandContext.UserOutput.Returns(userOutput);
            commandContext.NodeRpcClientFactory.Returns(nodeRpcFactory);

            var rpcNodeConfig = Substitute.For<IRpcNodeConfig>();
            rpcNodeConfig.NodeId = "node1";
            rpcNodeConfig.HostAddress = IPAddress.Any;
            rpcNodeConfig.PublicKey = "0";
            rpcNodeConfig.Port = 1337;

            commandContext.GetNodeConfig(Arg.Any<string>()).Returns(rpcNodeConfig);

            var commands = new List<ICommand> {new ConnectCommand(logger, commandContext)};
            var console = new CatalystCli(userOutput, commands);

            var isCommandParsed = console.ParseCommand("connect", "-n", "node1");
            isCommandParsed.Should().BeTrue();

            commandContext.SocketClientRegistry.Registry.Count.Should().Be(1);
            commandContext.UserOutput.Received(1).WriteLine($"Connected to Node {nodeRpcClient.Channel.RemoteAddress}");
        }

        [Fact]
        public void Cannot_Connect_With_Invalid_Config()
        {
            var logger = Substitute.For<ILogger>();
            var userOutput = Substitute.For<IUserOutput>();
            var commandContext = Substitute.For<ICommandContext>();

            commandContext.GetNodeConfig(Arg.Any<string>()).Returns((IRpcNodeConfig) null);

            var commands = new List<ICommand> {new ConnectCommand(logger, commandContext)};
            var console = new CatalystCli(userOutput, commands);

            var exception = Record.Exception(() => console.ParseCommand("connect", "-n", "node1"));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Cannot_Connect_With_Invalid_SocketChannel()
        {
            var logger = Substitute.For<ILogger>();
            var userOutput = Substitute.For<IUserOutput>();
            var clientSocketRegistry = new SocketClientRegistry<INodeRpcClient>(_testScheduler);

            var commandContext = Substitute.For<ICommandContext>();
            commandContext.SocketClientRegistry.Returns(clientSocketRegistry);
            commandContext.UserOutput.Returns(userOutput);

            var rpcNodeConfig = Substitute.For<IRpcNodeConfig>();
            rpcNodeConfig.NodeId = "node1";
            rpcNodeConfig.HostAddress = IPAddress.Any;
            rpcNodeConfig.PublicKey = "0";
            commandContext.GetNodeConfig(Arg.Any<string>()).Returns(rpcNodeConfig);

            var commands = new List<ICommand> {new ConnectCommand(logger, commandContext)};
            var console = new CatalystCli(userOutput, commands);

            var isCommandParsed = console.ParseCommand("connect", "-n", "node1");
            isCommandParsed.Should().BeFalse();

            commandContext.UserOutput.Received(1).WriteLine(ConnectCommand.InvalidSocketChannel);

            var connectCommand = new ConnectCommand(logger, commandContext);
        }
    }
}
