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
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.TestUtils;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.IntegrationTests
{
    public class TestCatalystNode : ConfigFileBasedTest, ICatalystNode
    {
        public string Name { get; }
        private ILifetimeScope _scope;
        private ICatalystNode _catalystNode;

        private readonly IEnumerable<string> _configFilesUsed = new[]
        {
            Constants.NetworkConfigFile(Network.Dev),
            Constants.ComponentsJsonConfigFile,
            Constants.SerilogJsonConfigFile
        }.Select(f => Path.Combine(Constants.ConfigSubFolder, f));

        private ContainerProvider _configProvider;

        public IConsensus Consensus => _catalystNode.Consensus;

        public TestCatalystNode(string name, ITestOutputHelper output) 
            : base(new[]
            {
                Constants.NetworkConfigFile(Network.Dev),
                Constants.ComponentsJsonConfigFile,
                Constants.SerilogJsonConfigFile
            }.Select(f => Path.Combine(Constants.ConfigSubFolder, f)), output)
        {
            Name = name;
            _configProvider = new ContainerProvider(_configFilesUsed, FileSystem, output);
        }

        public async Task RunAsync(CancellationToken cancellationSourceToken)
        {
            if (_catalystNode == null)
            {
                BuildNode();
            }

            await _catalystNode.RunAsync(cancellationSourceToken);
        }

        protected virtual void OverrideContainerBuilderRegistrations() { }

        public void BuildNode()
        {
            _configProvider.ConfigureContainerBuilder();

            OverrideContainerBuilderRegistrations();

            _scope = _configProvider.Container.BeginLifetimeScope(CurrentTestName);
            _catalystNode = _scope.Resolve<ICatalystNode>();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _configProvider.Dispose();
            _scope.Dispose();
        }
    }
}
