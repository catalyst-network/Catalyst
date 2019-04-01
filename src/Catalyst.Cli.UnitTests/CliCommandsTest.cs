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
using System.Collections.Generic;

using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Helpers.FileSystem;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Cli.UnitTests.TestUtils;

using Xunit;
using Xunit.Abstractions;
using Moq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using FluentAssertions;

namespace Catalyst.Cli.UnitTests
{
    public class CliCommandsTests : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;
        
        //private ICertificateStore _certificateStore;
        
        private readonly string LifetimeTag;
        
        //private RpcClient _rpcClient;
        private ICatalystCli _shell;
        
        public CliCommandsTests(ITestOutputHelper output) : base(output)
        {
            var targetConfigFolder = new FileSystem().GetCatalystHomeDir().FullName;
                
            // check if user home data dir has a shell config
            var shellComponentsFilePath = Path.Combine(targetConfigFolder, Constants.ShellComponentsJsonConfigFile);
            var shellSeriLogFilePath = Path.Combine(targetConfigFolder, Constants.ShellSerilogJsonConfigFile);
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
               .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellSerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellNodesConfigFile))
               .Build();
            
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            LifetimeTag = declaringType.AssemblyQualifiedName;
        }
        
        //This test is the base to all other tests.  If the Cli cannot connect to a node than all other commands
        //will fail
        [Fact]
        public void CanConnectToNode()
        {
            //Create ContainerBuilder based on the configuration
            ConfigureContainerBuilder(_config);
            
            var serviceCollection = new ServiceCollection();

            var container = ContainerBuilder.Build();

            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                _shell = container.Resolve<ICatalystCli>();
                
                var hasConnected = _shell.Ads.OnCommand("connect", "node", "node1");
                hasConnected.Should().BeTrue();
            }
        }
        
        //This test is the base to all other tests.  If the Cli cannot connect to a node than all other commands
        //will fail
        [Fact]
        public void CanHandleMultipleConnectionAttempts()
        {
            //Create ContainerBuilder based on the configuration
            ConfigureContainerBuilder(_config);
            
            var serviceCollection = new ServiceCollection();

            var container = ContainerBuilder.Build();

            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {               
                _shell = container.Resolve<ICatalystCli>();
                
                var hasConnected = _shell.Ads.OnCommand("connect", "node", "node1");
                hasConnected.Should().BeTrue();

                for (int i = 0; i < 10; i++)
                {
                    var canConnect = _shell.Ads.OnCommand("connect", "node", "node1");
                    canConnect.Should().BeTrue();
                }
            }
        }

        [Fact(Skip = "Not ready yet.")]
        public void CanHandleSslCertificateWrongPassword()
        {
            //Create ContainerBuilder based on the configuration
            ConfigureContainerBuilder(_config);
            
            var serviceCollection = new ServiceCollection();

            var container = ContainerBuilder.Build();

            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                _shell = container.Resolve<ICatalystCli>();

                var certificateStore = new Mock<ICertificateStore>();
                //certificateStore.Setup(x => x.ReadOrCreateCertificateFile()).Returns(false);
                    
                var hasConnected = _shell.Ads.OnCommand("connect", "node", "node1");
                hasConnected.Should().BeTrue();
            }
        }

        [Fact(Skip = "Not ready yet.")]
        public void CanGetNodeConfig()
        {
            //Create ContainerBuilder based on the configuration
            ConfigureContainerBuilder(_config);
            
            var serviceCollection = new ServiceCollection();

            var container = ContainerBuilder.Build();

            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                _shell = container.Resolve<ICatalystCli>();

                
                if (!_shell.Ads.IsConnectedNode("node1"))
                {
                    var hasConnected = _shell.Ads.OnCommand("connect", "node", "node1");
                    hasConnected.Should().BeTrue();
                }

                var node1 = _shell.Ads.GetConnectedNode("node1");
                node1.Should().NotBeNull("we've just connected it");
                
                var result = _shell.Ads.OnCommand("get", "config", "node1");
                result.Should().BeTrue();
            }
        }
        
        [Fact(Skip = "Not ready yet.")]
        public void TryToGetNodeConfigWithoutConnectingToNode()
        {
            //Create ContainerBuilder based on the configuration
            ConfigureContainerBuilder(_config);
            
            var serviceCollection = new ServiceCollection();

            var container = ContainerBuilder.Build();

            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                _shell = container.Resolve<ICatalystCli>();
                
                var result = _shell.Ads.OnCommand("get", "config", "node1");
                result.Should().BeFalse();
            }
        }
        

        [Fact(Skip = "Not ready yet.")]
        public void CanGetVersion()
        {
            //Create ContainerBuilder based on the configuration
            ConfigureContainerBuilder(_config);
            
            var serviceCollection = new ServiceCollection();

            var container = ContainerBuilder.Build();

            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                _shell = container.Resolve<ICatalystCli>();
                
                if (!_shell.Ads.IsConnectedNode("node1"))
                {
                    var hasConnected = _shell.Ads.OnCommand("connect", "node", "node1");
                    hasConnected.Should().BeTrue();
                }
                
                var node1 = _shell.Ads.GetConnectedNode("node1");
                node1.Should().NotBeNull("we've just connected it");
                
                
                var result = _shell.Ads.OnCommand("get", "version", "node1");
                result.Should().BeTrue();
            }
        }
    }
}