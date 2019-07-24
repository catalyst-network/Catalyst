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
using System.Collections.Generic;
using System.IO;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.Modules.Contract;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.Modules.Ledger;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Core.Lib.Modules.Contract;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using LedgerService = Catalyst.Core.Lib.Modules.Ledger.Ledger;

namespace Catalyst.Node.UnitTests.Config
{
    public sealed class JsonConfiguredModuleTests : ConfigFileBasedTest
    {
        protected override IEnumerable<string> ConfigFilesUsed { get; }

        public JsonConfiguredModuleTests(ITestOutputHelper output) : base(output)
        {
            ConfigFilesUsed = new[] {Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile)};

            ConfigureContainerBuilder();

            ContainerBuilder.RegisterInstance(PeerSettingsHelper.TestPeerSettings())
               .As<IPeerSettings>();
        }

        [Theory]
        [InlineData(typeof(IConsensus), typeof(Catalyst.Core.Lib.Modules.Consensus.Consensus))]
        [InlineData(typeof(IContract), typeof(Contract))]
        [InlineData(typeof(IDfs), typeof(Catalyst.Core.Lib.Modules.Dfs.Dfs))]
        [InlineData(typeof(ILedger), typeof(LedgerService))]
        [InlineData(typeof(IMempool), typeof(Catalyst.Core.Lib.Modules.Mempool.Mempool))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        private void ComponentsJsonFile_should_configure_modules(Type interfaceType, Type resolutionType)
        {
            using (var container = ContainerBuilder.Build())
            using (var scope = container.BeginLifetimeScope(Guid.NewGuid().ToString()))
            {
                var resolvedType = scope.Resolve(interfaceType);
                resolvedType.Should().NotBeNull();
                resolvedType.Should().BeOfType(resolutionType);
            }
        }
    }
}
