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
using Autofac.Configuration;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Events;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Config;
using Catalyst.Core.IO.Events;
using Catalyst.Core.P2P.IO.Observers;
using Catalyst.Core.Util;
using Catalyst.Protocol.Interfaces.Validators;
using Catalyst.Protocol.Validators;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.POA.CE.UnitTests.Config
{
    public sealed class ComponentsConfigTests
    {
        private readonly string _componentsConfig;

        public ComponentsConfigTests()
        {
            _componentsConfig = Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile);
        }

        private static IContainer ConfigureAndBuildContainer(params string[] configFiles)
        {
            var configBuilder = new ConfigurationBuilder();
            configFiles.ToList().ForEach(f => configBuilder.AddJsonFile(f));

            configBuilder.AddJsonFile(Path.Combine(Constants.ConfigSubFolder, NetworkTypes.Dev + ".json"));

            var configRoot = configBuilder.Build();
            var configModule = new ConfigurationModule(configRoot);

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(configModule);
            containerBuilder.RegisterInstance(configRoot).As<IConfigurationRoot>();

            var logger = Substitute.For<ILogger>();
            containerBuilder.RegisterInstance(logger).As<ILogger>();

            var passwordReader = new TestPasswordReader();
            containerBuilder.RegisterInstance(passwordReader).As<IPasswordReader>();
            
            var keyRegistry = TestKeyRegistry.MockKeyRegistry();
            containerBuilder.RegisterInstance(keyRegistry).As<IKeyRegistry>();

            var keyStore = Substitute.For<IKeyStore>();
            containerBuilder.RegisterInstance(keyStore).As<IKeyStore>();

            containerBuilder.RegisterType<TransactionValidator>().As<ITransactionValidator>();
            containerBuilder.RegisterType<TransactionReceivedEvent>().As<ITransactionReceivedEvent>();
            
            var container = containerBuilder.Build();
            return container;
        }

        [Fact]
        public void Components_config_should_allow_resolving_IPeerSettings()
        {
            var container = ConfigureAndBuildContainer(_componentsConfig);
            var resolved = container.Resolve<IPeerSettings>();
            resolved.PublicKey.Should().Be(TestKeyRegistry.TestPublicKey);
        }

        [Fact]
        public void Components_config_should_allow_resolving_IPeerIdentifier()
        {
            var container = ConfigureAndBuildContainer(_componentsConfig);
            var resolved = container.Resolve<IPeerIdentifier>();

            resolved.PublicKey.KeyToString().Should()
               .Be(TestKeyRegistry.TestPublicKey);
        }

        [Fact]
        public void Components_config_should_allow_resolving_Collection_of_MessageHandlers()
        {
            var container = ConfigureAndBuildContainer(_componentsConfig);

            var handlers = container.Resolve<IEnumerable<IP2PMessageObserver>>();
            handlers.Select(h => h.GetType()).Should().BeEquivalentTo(
                typeof(PingRequestObserver),
                typeof(PingResponseObserver),
                typeof(GetNeighbourRequestObserver),
                typeof(GetNeighbourResponseObserver),
                typeof(TransactionBroadcastObserver),
                typeof(DeltaDfsHashObserver),
                typeof(CandidateDeltaObserver),
                typeof(FavouriteDeltaObserver)
            );
        }
    }
}
