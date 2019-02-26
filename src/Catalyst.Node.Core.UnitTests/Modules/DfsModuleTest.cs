﻿using System;
using System.IO;
using Autofac;
using Catalyst.Node.Common.Modules.Consensus;
using Catalyst.Node.Common.Modules.Contract;
using Catalyst.Node.Common.Modules.Dfs;
using Catalyst.Node.Common.Modules.Gossip;
using Catalyst.Node.Common.Modules.Ledger;
using Catalyst.Node.Common.Modules.Mempool;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Config;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Node.Core.Modules.Contract;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Node.Core.Modules.Gossip;
using Catalyst.Node.Core.Modules.Ledger;
using Catalyst.Node.Core.Modules.Mempool;
using Catalyst.Node.Core.UnitTest.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Modules
{
    public class JsonConfiguredModuleTest : BaseModuleConfigTest
    {
        public JsonConfiguredModuleTest() 
            : base(Path.Combine(Constants.ConfigFolder, Constants.ComponentsJsonConfigFile)) {}

        [Theory]
        [InlineData(typeof(IConsensus), typeof(Consensus))]
        [InlineData(typeof(IContract), typeof(Contract))]
        [InlineData(typeof(IDfs), typeof(IpfsDfs))]
        [InlineData(typeof(IGossip), typeof(Gossip))]
        [InlineData(typeof(ILedger), typeof(Ledger))]
        [InlineData(typeof(IMempool), typeof(Mempool))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void ComponentsJsonFile_should_configure_modules(Type interfaceType, Type resolutionType)
        {
            var resolvedType = Container.Resolve(interfaceType);
            resolvedType.Should().NotBeNull();
            resolvedType.Should().BeOfType(resolutionType);
        }
    }
}
