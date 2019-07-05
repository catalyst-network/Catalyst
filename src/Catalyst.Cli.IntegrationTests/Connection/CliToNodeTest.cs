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
using Catalyst.Common.FileSystem;
using Catalyst.Common.Interfaces.Cryptography;
using Microsoft.Extensions.Configuration;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Catalyst.Common.Interfaces;
using System.Threading;
using NSubstitute;
using Serilog;
using Catalyst.TestUtils;
using Ipfs.CoreApi;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Cli.IntegrationTests.Commands;
using Constants = Catalyst.Common.Config.Constants;


namespace Catalyst.Cli.IntegrationTests.Connection
{
    public sealed class CliToNodeTest : CliCommandTestBase
    {
        private readonly NodeTest _node;

        public static readonly List<object[]> Networks = 
            Enumeration.GetAll<Network>().Select(n => new object[] { n }).ToList();

        public CliToNodeTest(ITestOutputHelper output) : base(output, false, true)
        {
            _node = new NodeTest(output);
        }
        public static Network GetNetworkType(string networkType)
        {
            Network netTemp = null;

            switch (networkType)
            {
                case "Test_Mode": { netTemp = Network.Test; } break;
                case "Dev_Mode": { netTemp = Network.Dev; } break;
                case "Main_Mode": { netTemp = Network.Main; } break;
                default:
                    throw new InvalidOperationException("This network type or mode is not valid");
            }
            return netTemp;
        }

        private class NodeTest : CliCommandTestBase, IDisposable
        {
            private CancellationTokenSource _cancellationSource;

            public NodeTest(ITestOutputHelper output) : base(output, false, false) { }


            private void NodeSetup(object network)
            {
                var currentNetwork = GetNetworkType(network as string);

                var configFiles = new[]
                    {
                    Constants.NetworkConfigFile(currentNetwork),
                    Constants.ComponentsJsonConfigFile,
                    Constants.SerilogJsonConfigFile
                }
                .Select(f => Path.Combine(Constants.ConfigSubFolder, f));

                var configBuilder = new ConfigurationBuilder();
                configFiles.ToList().ForEach(f => configBuilder.AddJsonFile(f));

                var configRoot = configBuilder.Build();
                ConfigureContainerBuilder(configRoot);
            }

            public void StartNode(string networkType)
            {
                var threadStart = new ParameterizedThreadStart(RunNodeInstance);
                var thread = new Thread(threadStart);
                thread.Start(networkType);
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
                passwordReader.ReadSecurePassword().ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("trendy"));
                var logger = Substitute.For<ILogger>();
                return new IpfsAdapter(passwordReader, peerSettings, FileSystem, logger);
            }

            private void RunNodeInstance(object network)
            {
                NodeSetup(network);

                _cancellationSource = new CancellationTokenSource();

                var ipfs = ConfigureKeyTestDependency();
                ContainerBuilder.RegisterInstance(ipfs).As<ICoreApi>();

                using (var container = ContainerBuilder.Build())
                {
                    using (var scope = container.BeginLifetimeScope(CurrentTestName))
                    {
                        var node = scope.Resolve<ICatalystNode>();
                        node.RunAsync(_cancellationSource.Token).Wait(_cancellationSource.Token);
                    }
                }
            }

            public void Dispose()
            {
                base.Dispose();
                _cancellationSource?.Dispose();                
            }
        }

        [Theory]
        [InlineData("Main_Mode")]
        public void CliToNode_Connect_To_Node(string modeType)
        {
            _node.StartNode(modeType);

            Thread.Sleep(4500);

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
    }
}
