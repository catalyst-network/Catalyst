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
using System.Net;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Core.P2P;
using NSubstitute;
using SharpRepository.Repository;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.UnitTests.TestUtils;
using DnsClient;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using SharpRepository.InMemoryRepository;
using Xunit;
using Xunit.Abstractions;
using Constants = Catalyst.Node.Common.Helpers.Config.Constants;
using Dns = Catalyst.Node.Common.Helpers.Network.Dns;
using Peer = Catalyst.Node.Common.P2P.Peer;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public sealed class PeerDiscoveryUnitTest : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;
        private readonly IDns _dns;
        private readonly IRepository<Peer> _peerRepository;
        private readonly ILogger _logger;
        private readonly ILookupClient _lookupClient;

        public PeerDiscoveryUnitTest(ITestOutputHelper output) : base(output)
        {
            _peerRepository = Substitute.For<IRepository<Peer>>();
            _logger = Substitute.For<ILogger>();
            _lookupClient = Substitute.For<ILookupClient>();
            _dns = new Dns(_lookupClient);

            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
        }

        [Fact] public void ResolvesIPeerDiscoveryCorrectly()
        {
            ConfigureContainerBuilder(_config);

            var container = ContainerBuilder.Build();
            using(var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var peerDiscovery = container.Resolve<IPeerDiscovery>();
                Assert.NotNull(peerDiscovery);
                peerDiscovery.Should().BeOfType(typeof(PeerDiscovery));
                Assert.NotNull(peerDiscovery.Dns);
                peerDiscovery.Dns.Should().BeOfType(typeof(DevDns));
                Assert.NotNull(peerDiscovery.Logger);
                peerDiscovery.Logger.Should().BeOfType(typeof(Logger));
                Assert.NotNull(peerDiscovery.SeedNodes);
                peerDiscovery.SeedNodes.Should().BeOfType(typeof(List<string>));
                Assert.NotNull(peerDiscovery.Peers);
                peerDiscovery.Peers.Should().BeOfType(typeof(List<IPEndPoint>));
                Assert.NotNull(peerDiscovery.PeerRepository);
                peerDiscovery.PeerRepository.Should().BeOfType(typeof(InMemoryRepository<Peer>));
            }
        }

        [Fact] public void CanParseDnsNodesFromConfig()
        {
            var urlList = new List<string>();
            var domain1 = "seed1.catalystnetwork.io";
            var domain2 = "seed1.catalystnetwork.io";
            urlList.Add(domain1);
            urlList.Add(domain2);

            MockQueryResponse.CreateFakeLookupResult(domain1, "192.0.2.1:42069", _lookupClient);
            MockQueryResponse.CreateFakeLookupResult(domain2, "192.0.2.2:42069", _lookupClient);

            var peerDiscovery = new PeerDiscovery(_dns, _peerRepository, _config, _logger);

            peerDiscovery.ParseDnsServersFromConfig(_config);
            peerDiscovery.SeedNodes.Should().NotBeNullOrEmpty();
            peerDiscovery.SeedNodes.Should().Contain(urlList);
        }

        [Fact] public async Task CanGetSeedNodesFromDns()
        {
            var urlList = new List<string>();
            var domain1 = "seed1.catalystnetwork.io";
            var domain2 = "seed1.catalystnetwork.io";
            urlList.Add(domain1);
            urlList.Add(domain2);

            MockQueryResponse.CreateFakeLookupResult(domain1, "192.0.2.2:42069", _lookupClient);
            MockQueryResponse.CreateFakeLookupResult(domain2, "192.0.2.2:42069", _lookupClient);

            var peerDiscovery = new PeerDiscovery(_dns, _peerRepository, _config, _logger);

            await peerDiscovery.GetSeedNodesFromDns(urlList);
            peerDiscovery.Peers.Should().NotBeNullOrEmpty();
            peerDiscovery.Peers.Should().HaveCount(3);
            peerDiscovery.Peers.Should().NotContainNulls();
            peerDiscovery.SeedNodes.Should().Contain(urlList);
            peerDiscovery.Peers.Should().ContainItemsAssignableTo<IPEndPoint>();
        }
    }
}
