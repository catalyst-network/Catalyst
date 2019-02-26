using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Autofac;
using Autofac.Core;
using Catalyst.Node.Common.Modules.Mempool;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Config;
using Catalyst.Node.Core.Modules.Mempool;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocols.Transaction;
using FluentAssertions;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Modules.Mempool
{
    public class MempoolModuleTest : BaseModuleConfigTest   
    {
        public MempoolModuleTest() : base(
            Path.Combine(Constants.ConfigFolder, Constants.ComponentsJsonConfigFile),
            PerformExtraRegistrations)
        {}

        public static void PerformExtraRegistrations(ContainerBuilder builder)
        {
            builder.RegisterType<InMemoryRepository<StTxModel, Key>>().As<IRepository<StTxModel, Key>>();
            builder.RegisterInstance(Substitute.For<ILogger>()).As<ILogger>();
        }

        [Theory]
        [InlineData(typeof(IMempool), typeof(Core.Modules.Mempool.Mempool))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void MempoolModule_should_configure_container(Type interfaceType, Type resolutionType)
        {
            var resolvedType = Container.Resolve(interfaceType);
            resolvedType.Should().NotBeNull();
            resolvedType.Should().BeOfType(resolutionType);
        }
    }
}
