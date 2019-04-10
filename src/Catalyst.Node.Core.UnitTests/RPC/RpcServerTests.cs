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
using System.Net.Sockets;
using Autofac;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Microsoft.Extensions.Configuration;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public class RpcServerTests : ConfigFileBasedTest, IDisposable
    {
        private readonly IConfigurationRoot _config;

        private IRpcServer _rpcServer;

        public RpcServerTests(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build(), CurrentTestName);
        }

        //TODO : this is the simplest test that can cause the build to hang
        //need to investigate and see if we can solve it
        [Fact(Skip = "causes build to hang")]
        // [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void ServerConnectedToCorrectPort()
        {
            //Create ContainerBuilder based on the configuration
            ConfigureContainerBuilder(_config);

            //Create container
            var container = ContainerBuilder.Build();
 
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var logger = container.Resolve<ILogger>();

                using (_rpcServer = container.Resolve<IRpcServer>())
                using (var client = new TcpClient(_rpcServer.Settings.BindAddress.ToString(),
                    _rpcServer.Settings.Port))
                {
                    client.Should().NotBeNull();
                    client.Connected.Should().BeTrue();
                }
            }

            container.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(!disposing) { return; }
            _rpcServer?.Dispose();
        }
    }
}
