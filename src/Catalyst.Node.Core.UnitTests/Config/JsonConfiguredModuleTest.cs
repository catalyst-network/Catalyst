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
using System.Net;
using Autofac;
using Autofac.Configuration;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.Modules.Contract;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.Modules.Ledger;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Node.Core.Modules.Contract;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;
using LedgerService = Catalyst.Node.Core.Modules.Ledger.Ledger;

namespace Catalyst.Node.Core.UnitTests.Config
{
    public sealed class JsonConfiguredModuleTest : IDisposable
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
            peerSettings.BindAddress.Returns(IPAddress.Parse("124.220.98.2"));
            peerSettings.Port.Returns(12);
            builder.RegisterInstance(peerSettings).As<IPeerSettings>();
        }

        public void Dispose()
        {
            _container?.Dispose();
        }

        [Theory]
        [InlineData(typeof(IConsensus), typeof(Core.Modules.Consensus.Consensus))]
        [InlineData(typeof(IContract), typeof(Contract))]
        [InlineData(typeof(IDfs), typeof(Core.Modules.Dfs.Dfs))]
        [InlineData(typeof(ILedger), typeof(LedgerService))]
        [InlineData(typeof(IMempool), typeof(Core.Modules.Mempool.Mempool))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        private void ComponentsJsonFile_should_configure_modules(Type interfaceType, Type resolutionType)
        {
            using (_container.BeginLifetimeScope(Guid.NewGuid()))
            {
                var resolvedType = _container.Resolve(interfaceType);
                resolvedType.Should().NotBeNull();
                resolvedType.Should().BeOfType(resolutionType);
            }
        }
    }
}
