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

using Autofac;
using Catalyst.Common.Interfaces.Cli;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using System;
using Catalyst.Common.Interfaces.Cryptography;
using Microsoft.Extensions.Configuration;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Common.Config;
using System.Linq;
using System.IO;
using Catalyst.Common.Interfaces;
using NSubstitute;
using Serilog;
using Catalyst.TestUtils;
using Ipfs.CoreApi;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Cli.IntegrationTests.Commands;
using Constants = Catalyst.Common.Config.Constants;

namespace Catalyst.Cli.IntegrationTests.Connection
{
    public sealed class CliToNodeTests : CliCommandTestBase, IDisposable
    {
        private readonly NodeTest _node;

        public CliToNodeTests(ITestOutputHelper output) : base(output, false)
        {
            _node = new NodeTest(output);
        }

        private sealed class NodeTest : ConfigFileBasedTest, IDisposable
        {
            private ILifetimeScope _scope;
            private IContainer _container;

            public NodeTest(ITestOutputHelper output) : base(output) { }

            private void NodeSetup()
            {
                var configFiles = new[]
                {
                    Constants.NetworkConfigFile(Network.Main),
                    Constants.ComponentsJsonConfigFile,
                    Constants.SerilogJsonConfigFile
                }.Select(f => Path.Combine(Constants.ConfigSubFolder, f));

                var configBuilder = new ConfigurationBuilder();
                configFiles.ToList().ForEach(f => configBuilder.AddJsonFile(f));

                var configRoot = configBuilder.Build();
                ConfigureContainerBuilder(configRoot);
            }

            private IpfsAdapter ConfigureKeyTestDependency()
            {
                var peerSettings = Substitute.For<IPeerSettings>();
                peerSettings.SeedServers.Returns(new[]
                {
                    "seed1.server.va",
                    "island.domain.tv"
                });

                var passwordReader = Substitute.For<IPasswordReader>();
                passwordReader.ReadSecurePasswordAndAddToRegistry(Arg.Any<PasswordRegistryKey>(), Arg.Any<string>()).ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("trendy"));
                var logger = Substitute.For<ILogger>();
                return new IpfsAdapter(passwordReader, peerSettings, FileSystem, logger);
            }

            public void RunNodeInstance()
            {
                NodeSetup();

                var ipfs = ConfigureKeyTestDependency();
                ContainerBuilder.RegisterInstance(ipfs).As<ICoreApi>();

                _container = ContainerBuilder.Build();

                _scope = _container.BeginLifetimeScope(CurrentTestName);
                _ = _scope.Resolve<ICatalystNode>();
            }

            public new void Dispose()
            {
                base.Dispose();

                _container.Dispose();
                _scope.Dispose();
            }
        }

        [Fact]
        public void CliToNode_Connect_To_Node()
        {
            _node.RunNodeInstance();

            using (var container = ContainerBuilder.Build())
            {
                using (container.BeginLifetimeScope(CurrentTestName))
                {
                    var shell = container.Resolve<ICatalystCli>();
                    var hasConnected = shell.ParseCommand("connect", "-n", "node1");
                    hasConnected.Should().BeTrue();
                }
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            _node?.Dispose();
        }
    }
}
