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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.TestUtils;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Cli.UnitTests.Config
{
    public sealed class GlobalConfigTests : ConfigFileBasedTest
    {
        public static readonly List<object[]> Networks =
            Enumeration.GetAll<Network>().Select(n => new object[] {n}).ToList();

        public GlobalConfigTests(ITestOutputHelper output) : base(output) { }

        [Theory(Skip = "Blocking CI")]
        [MemberData(nameof(Networks))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RegisteringAllConfigsShouldAllowResolvingCatalystNode(Network network)
        {
            var configFiles = new[]
                {
                    Constants.NetworkConfigFile(network),
                    Constants.ShellComponentsJsonConfigFile,
                    Constants.SerilogJsonConfigFile,
                    Constants.ShellNodesConfigFile,
                    Constants.ShellConfigFile
                }
               .Select(f => Path.Combine(Constants.ConfigSubFolder, f));

            var configBuilder = new ConfigurationBuilder();
            configFiles.ToList().ForEach(f => configBuilder.AddJsonFile(f));
            var configRoot = configBuilder.Build();

            ConfigureContainerBuilder(configRoot);

            var container = ContainerBuilder.Build();

            using (var scope = container.BeginLifetimeScope(CurrentTestName + network.Name))
            {
                scope.Resolve<ICatalystCli>();
            }
        }
    }
}
