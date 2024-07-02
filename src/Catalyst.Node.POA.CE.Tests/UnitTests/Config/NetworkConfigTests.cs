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
using Catalyst.Abstractions.Config;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.P2P;
using Catalyst.Protocol.Network;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SharpRepository.Repository;
using NUnit.Framework;
using NSubstitute;
using Lib.P2P;
using Catalyst.Abstractions.Dfs.CoreApi;
using Newtonsoft.Json.Linq;
using MultiFormats;

namespace Catalyst.Node.POA.CE.Tests.UnitTests.Config
{
    public sealed class NetworkConfigTests
    {
        public static readonly List<object[]> NetworkFiles;

        static NetworkConfigTests()
        {
            NetworkFiles = new List<NetworkType> { NetworkType.Devnet, NetworkType.Mainnet, NetworkType.Testnet }
               .Select(n => new[]
                {
                    Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(n)) as object
                }).ToList();
        }

        [TestCaseSource(nameof(NetworkFiles))]
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

        [TestCaseSource(nameof(NetworkFiles))]
        public void Network_config_should_allow_building_PeerSettings(string networkConfig)
        {
            var configRoot = new ConfigurationBuilder().AddJsonFile(networkConfig).Build();

            var configModule = new ConfigurationModule(configRoot);

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(configModule);
            containerBuilder.RegisterInstance(configRoot).As<IConfigurationRoot>();

            var address = new MultiAddress("/ip4/192.168.0.181/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdwu2yu3QMzKa2BDkb5C5pcuhtrH1G9HHbztbbxA8tGmf4");
            var peer = new Peer
            {
                PublicKey = "CAESLDAqMAUGAytlcAMhADyXIeZUUBKx3OiDdhDb5GGrDUPOhhzJWPf80Iqam3lr",
                Addresses = new[] { address },
                Id = address.PeerId
            };

            var config = Substitute.For<IConfigApi>();
            var networkTypeProvider = Substitute.For<INetworkTypeProvider>();
            networkTypeProvider.NetworkType.Returns(NetworkType.Devnet);
            var swarm = JToken.FromObject(new List<string> { $"/ip4/0.0.0.0/tcp/4100" });
            config.GetAsync("Addresses.Swarm").Returns(swarm);
            var peerSettings = new PeerSettings(configRoot, peer, config, networkTypeProvider);

            peerSettings.Should().NotBeNull();
            peerSettings.NetworkType.Should().NotBe(null);
            peerSettings.Port.Should().BeInRange(1025, 65535);
            peerSettings.BindAddress.Should().BeOfType<IPAddress>();
            peerSettings.PublicKey.Should().NotBeNullOrWhiteSpace();
            peerSettings.SeedServers.Should().NotBeEmpty();
        }
    }
}
