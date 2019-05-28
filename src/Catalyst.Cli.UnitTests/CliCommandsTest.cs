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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Config;
using Catalyst.Common.UnitTests.TestUtils;
using Xunit;
using Xunit.Abstractions;
using NSubstitute;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Autofac;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.Rpc;
using DotNetty.Transport.Channels;

namespace Catalyst.Cli.UnitTests
{
    public sealed class CliCommandsTests : ConfigFileBasedTest
    {
        private IContainer container;

        public CliCommandsTests(ITestOutputHelper output) : base(output)
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellConfigFile))
               .Build();

            var channel = Substitute.For<IChannel>();
            channel.Active.Returns(true);

            var nodeRpcClient = Substitute.For<INodeRpcClient>();
            nodeRpcClient.Channel.Returns(channel);
            nodeRpcClient.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));

            var nodeRpcClientFactory = Substitute.For<INodeRpcClientFactory>();
            nodeRpcClientFactory
               .GetClient(Arg.Any<X509Certificate>(), Arg.Is<IRpcNodeConfig>(c => c.NodeId == "node1"))
               .Returns(nodeRpcClient);

            ConfigureContainerBuilder(config);

            ContainerBuilder.RegisterInstance(nodeRpcClientFactory).As<INodeRpcClientFactory>();
            ContainerBuilder.RegisterInstance(new TestKeySigner()).As<IKeySigner>();
        }

        //This test is the base to all other tests.  If the Cli cannot connect to a node than all other commands
        //will fail
        [Fact]
        public void Cli_Can_Connect_To_Node()
        {
            using (var container = ContainerBuilder.Build())
            {
                using (container.BeginLifetimeScope(CurrentTestName))
                {
                    var shell = container.Resolve<ICatalystCli>();
                    var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
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
                        var canConnect = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                        canConnect.Should().BeTrue();
                    }
                }   
            }
        }

        [Fact]
        public void Cli_Can_Request_Node_Info()
        {
            using (var container = ContainerBuilder.Build())
            {
                using (container.BeginLifetimeScope(CurrentTestName))
                {
                    var shell = container.Resolve<ICatalystCli>();
                    var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                    hasConnected.Should().BeTrue();

                    var result = shell.AdvancedShell.ParseCommand("getinfo", "-i", "node1");
                    result.Should().BeTrue();
                }   
            }
        }

        [Fact]
        public void Cli_Can_Request_Node_Version()
        {
            using (var container = ContainerBuilder.Build())
            {
                using (container.BeginLifetimeScope(CurrentTestName))
                {
                    var shell = container.Resolve<ICatalystCli>();
                    var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                    hasConnected.Should().BeTrue();

                    var result = shell.AdvancedShell.ParseCommand("getversion", "-v", "node1");
                    result.Should().BeTrue();
                }   
            }
        }

        [Fact] 
        public void Cli_Can_Request_Node_Mempool()
        {
            using (var container = ContainerBuilder.Build())
            {
                using (container.BeginLifetimeScope(CurrentTestName))
                {
                    var shell = container.Resolve<ICatalystCli>();

                    var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                    hasConnected.Should().BeTrue();

                    var node1 = shell.AdvancedShell.GetConnectedNode("node1");
                    node1.Should().NotBeNull("we've just connected it");

                    var result = shell.AdvancedShell.ParseCommand("getmempool", "-m", "node1");
                    result.Should().BeTrue();
                }   
            }
        }

        [Fact] 
        public void Cli_Can_Request_Node_To_Sign_A_Message()
        {
            using (var container = ContainerBuilder.Build())
            {
                using (container.BeginLifetimeScope(CurrentTestName))
                {
                    var shell = container.Resolve<ICatalystCli>();
                
                    var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                    hasConnected.Should().BeTrue();

                    var result = shell.AdvancedShell.ParseCommand("sign", "-m", "test message", "-n", "node1");
                    result.Should().BeTrue();
                }   
            }
        }

        [Fact] 
        public void Cli_Can_Verify_Message()
        {
            using (var container = ContainerBuilder.Build())
            {
                using (container.BeginLifetimeScope(CurrentTestName))
                {
                    var shell = container.Resolve<ICatalystCli>();
                    var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                    hasConnected.Should().BeTrue();

                    var node1 = shell.AdvancedShell.GetConnectedNode("node1");
                    node1.Should().NotBeNull("we've just connected it");

                    var result = shell.AdvancedShell.ParseCommand(
                        "verify", "-m", "test message", "-k", "public_key", "-s", "signature", "-n", "node1");
                    result.Should().BeTrue();
                }   
            }
        }
    }
}
