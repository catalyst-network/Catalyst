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
using System.Linq;
using System.Reactive.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Helpers.FileSystem;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Cli.UnitTests.TestUtils;

using Xunit;
using Xunit.Abstractions;
using Moq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Autofac;
using Catalyst.Node.Common.Helpers.IO.Outbound;
using Catalyst.Node.Common.Helpers.Shell;
using DotNetty.Transport.Channels;
using FluentAssertions;

using Google.Protobuf.WellKnownTypes;
using NSubstitute;
using NSubstitute.Core;
using Serilog;
using Serilog.Extensions.Logging;

namespace Catalyst.Cli.UnitTests
{
    public sealed class CliCommandsTests : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;
        
        //private ICertificateStore _certificateStore;
        
        private readonly string LifetimeTag;
        
        //private RpcClient _rpcClient;
        private ICatalystCli _shell;
        private ILogger _logger;
        private ILifetimeScope _scope;
        
        /*private IRpcServer _rpcServer;
        private IRpcClient _rpcClient;*/
        
        public CliCommandsTests(ITestOutputHelper output) : base(output)
        {
            var targetConfigFolder = new FileSystem().GetCatalystHomeDir().FullName;
                
            // check if user home data dir has a shell config
            var shellComponentsFilePath = Path.Combine(targetConfigFolder, Constants.ShellComponentsJsonConfigFile);
            var shellSeriLogFilePath = Path.Combine(targetConfigFolder, Constants.SerilogJsonConfigFile);
            var shellNodesFilePath = Path.Combine(targetConfigFolder, Constants.ShellNodesConfigFile);
            
            if (!File.Exists(shellComponentsFilePath))
            {
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/shell.components.json"),
                    shellComponentsFilePath);
            }

            if (!File.Exists(shellSeriLogFilePath))
            {
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/shell.serilog.json"),
                    shellSeriLogFilePath);
            }
                
            if (!File.Exists(shellNodesFilePath))
            {
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/nodes.json"),
                    shellNodesFilePath);
            }
            
            _config = new ConfigurationBuilder()
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
            
            
            //Create ContainerBuilder based on the configuration
            ConfigureContainerBuilder(_config);
            ContainerBuilder.RegisterInstance(tcpClient).As<ISocketClient>();
            ContainerBuilder.RegisterInstance(client).As<IRpcClient>();
            
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            LifetimeTag = declaringType.AssemblyQualifiedName;
            
            var serviceCollection = new ServiceCollection();
            var container = ContainerBuilder.Build();
            
            _scope = container.BeginLifetimeScope(_currentTestName);
            
            _shell = container.Resolve<ICatalystCli>();
            
            _logger = container.Resolve<ILogger>();
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory.AddProvider(new SerilogLoggerProvider(_logger));
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
            var hasConnected = _shell.Ads.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = _shell.Ads.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");
            
            var result = _shell.Ads.ParseCommand("get", "-m", "node1");
            result.Should().BeTrue();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _scope.Dispose();
        }
    }
}