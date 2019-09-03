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
using Catalyst.Abstractions.Cli;
using Catalyst.Core.Config;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Cli.IntegrationTests.Config
{
    public sealed class GlobalConfigTests : FileSystemBasedTest
    {
        public static readonly List<object[]> Networks = 
            new List<Network> {Network.Devnet, Network.Mainnet, Network.Testnet}.Select(n => new object[] {n}).ToList();

        public GlobalConfigTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [MemberData(nameof(Networks))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Registering_All_Configs_Should_Allow_Resolving_ICatalystCli(Network network)
        {
            var configFilesUsed = new[]
                {
                    Constants.NetworkConfigFile(network),
                    Constants.ShellComponentsJsonConfigFile,
                    Constants.SerilogJsonConfigFile,
                    Constants.ShellNodesConfigFile,
                    Constants.ShellConfigFile
                }
               .Select(f => Path.Combine(Constants.ConfigSubFolder, f));

            using (var containerProvider = new ContainerProvider(configFilesUsed, FileSystem, Output))
            {
                containerProvider.ConfigureContainerBuilder();

                using (var scope = containerProvider.Container.BeginLifetimeScope(CurrentTestName + network))
                {
                    scope.Resolve<ICatalystCli>();
                }
            }
        }
    }
}
