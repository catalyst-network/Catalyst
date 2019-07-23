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
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Core.Lib.Modules.Dfs;
using Catalyst.TestUtils;
using Ipfs.CoreApi;
using NSubstitute;
using Serilog;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.IntegrationTests
{
    public class TestCatalystNode : ConfigFileBasedTest, ICatalystNode
    {
        public string Name { get; }
        private ILifetimeScope _scope;
        private IContainer _container;
        private ICatalystNode _catalystNode;

        protected override IEnumerable<string> ConfigFilesUsed { get; }

        public TestCatalystNode(string name, ITestOutputHelper output) : base(output)
        {
            Name = name;
            ConfigFilesUsed = new[]
            {
                Constants.NetworkConfigFile(Network.Main),
                Constants.ComponentsJsonConfigFile,
                Constants.SerilogJsonConfigFile
            }.Select(f => Path.Combine(Constants.ConfigSubFolder, f));
        }

        public async Task RunAsync(CancellationToken cancellationSourceToken)
        {
            if (_catalystNode == null)
            {
                BuildNode();
            }

            await _catalystNode.RunAsync(cancellationSourceToken);
        }

        private IpfsAdapter ConfigureKeyTestDependency()
        {
            var passwordReader = Substitute.For<IPasswordReader>();
            passwordReader.ReadSecurePasswordAndAddToRegistry(Arg.Any<PasswordRegistryKey>(), Arg.Any<string>())
               .ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("trendy"));
            var logger = Substitute.For<ILogger>();
            return new IpfsAdapter(passwordReader, FileSystem, logger);
        }

        public void BuildNode()
        {
            ConfigureContainerBuilder();

            var ipfs = ConfigureKeyTestDependency();
            ContainerBuilder.RegisterInstance(ipfs).As<ICoreApi>();

            _container = ContainerBuilder.Build();

            _scope = _container.BeginLifetimeScope(CurrentTestName);
            _catalystNode = _scope.Resolve<ICatalystNode>();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _container.Dispose();
            _scope.Dispose();
        }
    }
}
