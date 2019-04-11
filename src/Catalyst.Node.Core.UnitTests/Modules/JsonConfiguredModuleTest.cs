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
using Autofac.Configuration;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Modules.Consensus;
using Catalyst.Node.Common.Interfaces.Modules.Contract;
using Catalyst.Node.Common.Interfaces.Modules.Dfs;
using Catalyst.Node.Common.Interfaces.Modules.Ledger;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Node.Core.Modules.Contract;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Node.Core.Modules.Ledger;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Modules
{
    public sealed class JsonConfiguredModuleTest
    {
        private readonly IContainer _container;

        public JsonConfiguredModuleTest()
        {
            var configuration = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .Build();

            var configurationModule = new ConfigurationModule(configuration);
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(configuration).As<IConfigurationRoot>();
            containerBuilder.RegisterModule(configurationModule);

            PerformExtraRegistrations(containerBuilder);

            _container = containerBuilder.Build();
        }

        private static void PerformExtraRegistrations(ContainerBuilder builder)
        {
            builder.RegisterInstance(Substitute.For<ILogger>()).As<ILogger>();
            builder.RegisterInstance(new TestPasswordReader()).As<IPasswordReader>();

            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.SeedServers.Returns(new[] {"seed1.seedservers.bogus", "seed2.seedservers.bogus"});
            builder.RegisterInstance(peerSettings).As<IPeerSettings>();
        }

        [Theory]
        [InlineData(typeof(IConsensus), typeof(Consensus))]
        [InlineData(typeof(IContract), typeof(Contract))]
        [InlineData(typeof(IDfs), typeof(IpfsDfs))]
        [InlineData(typeof(ILedger), typeof(Ledger))]
        [InlineData(typeof(IMempool), typeof(Core.Modules.Mempool.Mempool))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        private void ComponentsJsonFile_should_configure_modules(Type interfaceType, Type resolutionType)
        {
            var resolvedType = _container.Resolve(interfaceType);
            resolvedType.Should().NotBeNull();
            resolvedType.Should().BeOfType(resolutionType);
            if (typeof(IDisposable).IsAssignableFrom(resolutionType))
            {
                ((IDisposable) resolvedType).Dispose();
            }
        }
    }
}
