﻿/*
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

﻿using System;
using System.IO;
using Autofac;
using Catalyst.Node.Common.Helpers.Config;
 using Catalyst.Node.Common.Interfaces.Modules.Consensus;
 using Catalyst.Node.Common.Interfaces.Modules.Contract;
 using Catalyst.Node.Common.Interfaces.Modules.Dfs;
 using Catalyst.Node.Common.Interfaces.Modules.Ledger;
 using Catalyst.Node.Common.Interfaces.Modules.Mempool;
 using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Node.Core.Modules.Contract;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Node.Core.Modules.Ledger;
using Catalyst.Node.Core.UnitTest.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Modules
{
    public class JsonConfiguredModuleTest : BaseModuleConfigTest
    {
        public JsonConfiguredModuleTest()
            : base(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile),
                PerformExtraRegistrations) { }

        private static void PerformExtraRegistrations(ContainerBuilder builder)
        {
            builder.RegisterInstance(Substitute.For<ILogger>()).As<ILogger>();
        }

        [Theory]
        [InlineData(typeof(IConsensus), typeof(Consensus))]
        [InlineData(typeof(IContract), typeof(Contract))]
        [InlineData(typeof(IDfs), typeof(Dfs))]
        [InlineData(typeof(ILedger), typeof(Ledger))]
        [InlineData(typeof(IMempool), typeof(Core.Modules.Mempool.Mempool))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        private void ComponentsJsonFile_should_configure_modules(Type interfaceType, Type resolutionType)
        {
            var resolvedType = Container.Resolve(interfaceType);
            resolvedType.Should().NotBeNull();
            resolvedType.Should().BeOfType(resolutionType);
        }
    }
}