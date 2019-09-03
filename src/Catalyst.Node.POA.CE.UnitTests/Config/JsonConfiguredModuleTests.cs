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
using Autofac;
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Config;
using Catalyst.Core.Dfs;
using Catalyst.Core.Ledger;
using Catalyst.Core.Mempool;
using Catalyst.Core.Mempool.Documents;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.POA.CE.UnitTests.Config
{
    public sealed class JsonConfiguredModuleTests : FileSystemBasedTest
    {
        private readonly ContainerProvider _containerProvider;

        public JsonConfiguredModuleTests(ITestOutputHelper output) : base(output)
        {
            var configFilesUsed = new[]
            {
                Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile)
            };

            _containerProvider = new ContainerProvider(configFilesUsed, FileSystem, Output);
            _containerProvider.ConfigureContainerBuilder();

            _containerProvider.ContainerBuilder.RegisterInstance(PeerSettingsHelper.TestPeerSettings())
               .As<IPeerSettings>();
        }

        [Theory]
        [InlineData(typeof(IConsensus), typeof(Core.Consensus.Consensus))]
        [InlineData(typeof(IDfs), typeof(DevDfs))]
        [InlineData(typeof(ILedger), typeof(Ledger))]
        [InlineData(typeof(IMempool<MempoolDocument>), typeof(Mempool))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        private void ComponentsJsonFile_should_configure_modules(Type interfaceType, Type resolutionType)
        {
            using (var scope = _containerProvider.Container.BeginLifetimeScope(Guid.NewGuid().ToString()))
            {
                var resolvedType = scope.Resolve(interfaceType);
                resolvedType.Should().NotBeNull();
                resolvedType.Should().BeOfType(resolutionType);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _containerProvider?.Dispose();
        }
    }
}
