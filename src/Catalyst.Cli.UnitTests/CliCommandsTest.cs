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

using System.IO;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Xunit;
using Xunit.Abstractions;
using Moq;
using NSubstitute;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Autofac;
using Catalyst.Node.Common.Interfaces.Rpc;
using Serilog;
using Serilog.Extensions.Logging;

namespace Catalyst.Cli.UnitTests
{
    public sealed class CliCommandsTests : ConfigFileBasedTest
    {
        public CliCommandsTests(ITestOutputHelper output) : base(output)
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellConfigFile))
               .Build();

            var nodeRpcClientFactory = Substitute.For<INodeRpcClientFactory>();
            var nodeRpcClient = Substitute.For<INodeRpcClient>();

            ConfigureContainerBuilder(config);

            ContainerBuilder.RegisterInstance(nodeRpcClientFactory).As<INodeRpcClientFactory>();
            ContainerBuilder.RegisterInstance(nodeRpcClient).As<INodeRpcClient>();
        }

        //This test is the base to all other tests.  If the Cli cannot connect to a node than all other commands
        //will fail
        [Fact]
        public void Cli_Can_Connect_To_Node()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();                    
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();
                scope.Dispose();
            }
        }

        [Fact]
        public void Cli_Can_Handle_Multiple_Connection_Attempts()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                for (int i = 0; i < 10; i++)
                {
                    var canConnect = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                    canConnect.Should().BeTrue();
                }

                scope.Dispose();
            }
        }

        [Fact]
        public void Cli_Can_Request_Node_Config()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var result = shell.AdvancedShell.ParseCommand("get", "-i", "node1");
                result.Should().BeTrue();
                scope.Dispose();
            }
        }

        [Fact]
        public void Cli_Can_Request_Node_Version()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var result = shell.AdvancedShell.ParseCommand("get", "-v", "node1");
                result.Should().BeTrue();
                scope.Dispose();
            }
        }

        [Fact]
        public void Cli_Can_Request_Node_Mempool()
        {
            var channel = Substitute.For<IChannel>();
            channel.Active.Returns(true);
            var tcpClient = Substitute.For<ISocketClient>();
            tcpClient.Channel.Returns(channel);


            var client = Substitute.For<IRpcClient>();
            client.GetClientSocketAsync(Arg.Any<IRpcNodeConfig>())
               .Returns(Task.FromResult(tcpClient));

            var hasConnected = _shell.Ads.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = _shell.Ads.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var result = _shell.Ads.ParseCommand("get", "-m", "node1");
            result.Should().BeTrue();
        }

        [Fact]
        public void Cli_Can_Request_Node_To_Sign_A_Message()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var result = shell.AdvancedShell.ParseCommand("sign", "-m", "test message", "-n", "node1");
                result.Should().BeTrue();
                scope.Dispose();
            }
            var node1 = _shell.Ads.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var result = _shell.Ads.ParseCommand("sign", "-m", "test message", "-n", "node1");
            result.Should().BeTrue();
        }

        [Fact]
        public void Cli_Can_Verify_Message()
        {
            var hasConnected = _shell.Ads.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = _shell.Ads.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var result = _shell.Ads.ParseCommand("verify", "-m", "test message", "-k", "public_key", "-s", "signature");
            result.Should().BeTrue();
        }
    }
}
