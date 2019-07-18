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
using System.Linq;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Core.Lib.Modules.Dfs;
using Catalyst.TestUtils;
using Ipfs.CoreApi;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit.Abstractions;

namespace Catalyst.Cli.IntegrationTests
{
    public class TestCatalystNode : ConfigFileBasedTest
    {
        public string Name { get; }
        private ILifetimeScope _scope;
        private IContainer _container;

        public TestCatalystNode(string name, ITestOutputHelper output) : base(output)
        {
            Name = name;
        }

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
            ConfigureContainerBuilder(configRoot, true, true);
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

        public void RunNodeInstance()
        {
            NodeSetup();

            var ipfs = ConfigureKeyTestDependency();
            ContainerBuilder.RegisterInstance(ipfs).As<ICoreApi>();

            _container = ContainerBuilder.Build();

            _scope = _container.BeginLifetimeScope(CurrentTestName);
            _ = _scope.Resolve<ICatalystNode>();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _container.Dispose();
            _scope.Dispose();
        }
    }
}
