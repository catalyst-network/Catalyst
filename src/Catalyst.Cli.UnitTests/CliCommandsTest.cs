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
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Helpers.FileSystem;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Cli.UnitTests.TestUtils;
using Xunit;
using Xunit.Abstractions;
using Moq;
using Microsoft.Extensions.Configuration;
using Autofac;
using FluentAssertions;

namespace Catalyst.Cli.UnitTests
{
    public sealed class CliCommandsTests : ConfigFileBasedTest, IDisposable
    {
        //private ICertificateStore _certificateStore;
        
        //private RpcClient _rpcClient;
        private readonly ICatalystCli _shell;
        private readonly ILifetimeScope _scope;

        public CliCommandsTests(ITestOutputHelper output) : base(output)
        {
            var targetConfigFolder = new FileSystem().GetCatalystHomeDir().FullName;

            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .Build();
            
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;

            ConfigureContainerBuilder(config);
            var container = ContainerBuilder.Build();
            _scope = container.BeginLifetimeScope(_currentTestName);
            _shell = container.Resolve<ICatalystCli>();
        }
        
        //This test is the base to all other tests.  If the Cli cannot connect to a node than all other commands
        //will fail
        [Fact]
        public void CanConnectToNode()
        {
            var hasConnected = _shell.Ads.OnCommand("connect", "node", "node1");
            hasConnected.Should().BeTrue();
        }
        
        //This test is the base to all other tests.  If the Cli cannot connect to a node than all other commands
        //will fail
        [Fact]
        public void CanHandleMultipleConnectionAttempts()
        {
            var hasConnected = _shell.Ads.OnCommand("connect", "node", "node1");
            hasConnected.Should().BeTrue();

            for (int i = 0; i < 10; i++)
            {
                var canConnect = _shell.Ads.OnCommand("connect", "node", "node1");
                canConnect.Should().BeTrue();
            }
        }

        [Fact(Skip = "Not ready yet.")]
        public void CanHandleSslCertificateWrongPassword()
        {
            var certificateStore = new Mock<ICertificateStore>();
            //certificateStore.Setup(x => x.ReadOrCreateCertificateFile()).Returns(false);
                
            var hasConnected = _shell.Ads.OnCommand("connect", "node", "node1");
            hasConnected.Should().BeTrue();
        }

        [Fact(Skip = "Not ready yet.")]
        public void CanGetNodeConfig()
        {
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
        
        [Fact(Skip = "Not ready yet.")]
        public void TryToGetNodeConfigWithoutConnectingToNode()
        {
            var result = _shell.Ads.OnCommand("get", "config", "node1");
            result.Should().BeFalse();
        }
        
        [Fact(Skip = "Not ready yet.")]
        public void CanGetVersion()
        {
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _scope.Dispose();
        }
    }
}