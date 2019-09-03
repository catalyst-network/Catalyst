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
using Catalyst.Abstractions;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Config;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.POA.CE.IntegrationTests.Config
{
    public class GlobalConfigTests : FileSystemBasedTest
    {
        public static readonly List<object[]> Networks = 
            new List<Network> {Network.Devnet, Network.Mainnet, Network.Testnet}.Select(n => new object[] {n}).ToList();

        private IEnumerable<string> _configFilesUsed;
        private ContainerProvider _containerProvider;

        public GlobalConfigTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [MemberData(nameof(Networks))]
        public void Registering_All_Configs_Should_Allow_Resolving_CatalystNode(Network network)
        {
            _configFilesUsed = new[]
                {
                    Constants.NetworkConfigFile(network),
                    Constants.ComponentsJsonConfigFile,
                    Constants.SerilogJsonConfigFile
                }
               .Select(f => Path.Combine(Constants.ConfigSubFolder, f));

            _containerProvider = new ContainerProvider(_configFilesUsed, FileSystem, Output);

            SocketPortHelper.AlterConfigurationToGetUniquePort(_containerProvider.ConfigurationRoot, CurrentTestName);

            _containerProvider.ConfigureContainerBuilder();

            _containerProvider.ContainerBuilder.RegisterInstance(new TestPasswordReader()).As<IPasswordReader>();

            using (var scope = _containerProvider.Container.BeginLifetimeScope(CurrentTestName + network))
            {
                _ = scope.Resolve<ICatalystNode>();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            _containerProvider?.Dispose();
        }
    }
}
