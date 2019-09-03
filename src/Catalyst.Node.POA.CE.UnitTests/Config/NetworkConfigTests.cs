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
using System.Net;
using Autofac;
using Autofac.Configuration;
using Catalyst.Core.Config;
using Catalyst.Core.P2P;
using Catalyst.Protocol.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.POA.CE.UnitTests.Config
{
    public sealed class NetworkConfigTests
    {
        public static readonly List<object[]> NetworkFiles;

        static NetworkConfigTests()
        {
            NetworkFiles = new List<Network> {Network.Devnet, Network.Mainnet, Network.Testnet}
               .Select(n => new[]
                {
                    Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(n)) as object
                }).ToList();
        }

        [Theory]
        [MemberData(nameof(NetworkFiles))]
        public void Network_Config_Should_Contain_a_valid_storage_module(string networkConfig)
        {
            var networkConfiguration = new ConfigurationBuilder().AddJsonFile(networkConfig).Build();
            var configurationSection = networkConfiguration
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration");
            var persistenceConfiguration = RepositoryFactory.BuildSharpRepositoryConfiguation(configurationSection);

            persistenceConfiguration.HasRepository.Should().BeTrue();
            persistenceConfiguration.DefaultRepository.Should().NotBeNullOrEmpty();
            persistenceConfiguration.DefaultRepository.Should().Be("inMemoryNoCaching");
        }

        [Theory]
        [MemberData(nameof(NetworkFiles))]
        public void Network_config_should_allow_building_PeerSettings(string networkConfig)
        {
            var configRoot = new ConfigurationBuilder().AddJsonFile(networkConfig).Build();

            var configModule = new ConfigurationModule(configRoot);

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(configModule);
            containerBuilder.RegisterInstance(configRoot).As<IConfigurationRoot>();

            var peerSettings = new PeerSettings(configRoot);

            peerSettings.Should().NotBeNull();
            peerSettings.Network.Should().NotBeNull();
            peerSettings.Port.Should().BeInRange(1025, 65535);
            peerSettings.BindAddress.Should().BeOfType<IPAddress>();
            peerSettings.PublicKey.Should().NotBeNullOrWhiteSpace();
            peerSettings.SeedServers.Should().NotBeEmpty();
        }
    }
}
