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
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Helpers.FileSystem;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Xunit;
using Xunit.Abstractions;
using Moq;
using NSubstitute;
using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Autofac;
using Catalyst.Node.Common.Helpers.Shell;
using DotNetty.Transport.Channels;
using Serilog;
using Serilog.Extensions.Logging;


namespace Catalyst.Cli.UnitTests
{
    public sealed class CliCommandsTests : ConfigFileBasedTest
    {
        private readonly ICatalystCli _shell;
        private readonly ILifetimeScope _scope;

        public CliCommandsTests(ITestOutputHelper output) : base(output)
        {
            var targetConfigFolder = FileSystem.GetCatalystHomeDir().FullName;

            new CliConfigCopier().RunConfigStartUp(targetConfigFolder, Network.Dev);

            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(targetConfigFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellNodesConfigFile))
               .Build();

            var channel = Substitute.For<IChannel>();
            channel.Active.Returns(true);
            var tcpClient = Substitute.For<ISocketClient>();
            tcpClient.Channel.Returns(channel);


            var client = Substitute.For<IRpcClient>();
            client.GetClientSocketAsync(Arg.Any<IRpcNodeConfig>())
               .Returns(Task.FromResult(tcpClient));
            client.ConnectToNode(Arg.Any<string>(), Arg.Any<IRpcNodeConfig>())
               .Returns(ci => new RpcNode((IRpcNodeConfig) ci[1], tcpClient));

            ConfigureContainerBuilder(config);
            ContainerBuilder.RegisterInstance(tcpClient).As<ISocketClient>();
            ContainerBuilder.RegisterInstance(client).As<IRpcClient>();

            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            var serviceCollection = new ServiceCollection();
            var container = ContainerBuilder.Build();
            _scope = container.BeginLifetimeScope(CurrentTestName);

            _shell = container.Resolve<ICatalystCli>();

            var logger = container.Resolve<ILogger>();
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory.AddProvider(new SerilogLoggerProvider(logger));
        }

        //This test is the base to all other tests.  If the Cli cannot connect to a node than all other commands
        //will fail
        [Fact]
        public void Cli_Can_Connect_To_Node()
        {
            using (_scope)
            {
                var hasConnected = _shell.Ads.ParseCommand("connect", "-n", "node1");

                hasConnected.Should().BeTrue();
            }
        }

        //This test is the base to all other tests.  If the Cli cannot connect to a node than all other commands
        //will fail
        [Fact]
        public void Cli_Can_Handle_Multiple_Connection_Attempts()
        {
            using (_scope)
            {
                var hasConnected = _shell.Ads.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                for (int i = 0; i < 10; i++)
                {
                    var canConnect = _shell.Ads.ParseCommand("connect", "-n", "node1");
                    canConnect.Should().BeTrue();
                }
            }
        }

        [Fact(Skip = "Not ready yet.")]
        public void CanHandleSslCertificateWrongPassword()
        {
            using (_scope)
            {
                var certificateStore = new Mock<ICertificateStore>();

                var hasConnected = _shell.Ads.OnCommand("connect", "node", "node1");
                hasConnected.Should().BeTrue();
            }
        }

        [Fact]
        public void Cli_Can_Request_Node_Config()
        {
            _shell.Ads.AskForUserInput(false);

            var hasConnected = _shell.Ads.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = _shell.Ads.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var result = _shell.Ads.ParseCommand("get", "-i", "node1");
            result.Should().BeTrue();
        }


        [Fact]
        public void Cli_Can_Request_Node_Version()
        {
            _shell.Ads.AskForUserInput(false);

            var hasConnected = _shell.Ads.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = _shell.Ads.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var result = _shell.Ads.ParseCommand("get", "-v", "node1");
            result.Should().BeTrue();
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
            var hasConnected = _shell.Ads.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = _shell.Ads.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var result = _shell.Ads.ParseCommand("sign", "-m", "test message", "-n", "node1");
            result.Should().BeTrue();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _scope.Dispose();
        }
    }
}
