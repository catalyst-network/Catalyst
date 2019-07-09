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
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.P2P;
using Catalyst.TestUtils;
using DnsClient;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.P2P
{
    public sealed class PeerSettingsTests : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;

        public PeerSettingsTests(ITestOutputHelper output) : base(output)
        {
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Test)))
               .Build();
        }

        [Fact]
        private void CanResolveIPeerSettings()
        {
            ConfigureContainerBuilder(_config);

            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var peerDiscovery = container.Resolve<IPeerSettings>();
                peerDiscovery.Network.Name.Should().Equals("testnet");
            }
        }
        
        [Fact]
        public void CanParseDnsNodesFromConfig()
        {
            var peerRepository = Substitute.For<IRepository<Peer>>();
            var lookupClient = Substitute.For<ILookupClient>();
            var dns = new Common.Network.DnsClient(lookupClient, Substitute.For<IPeerIdValidator>());
            
            var dnsDomains = new List<string>
            {
                "seed1.catalystnetwork.io",
                "seed2.catalystnetwork.io",
                "seed3.catalystnetwork.io",
                "seed4.catalystnetwork.io",
                "seed5.catalystnetwork.io"
            };
            
            var seedPid = "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839323232323232323232323232";
            
            dnsDomains.ForEach(domain =>
            {
                MockQueryResponse.CreateFakeLookupResult(domain, seedPid, lookupClient);
            });
            
            var peerSettings = new PeerSettings(_config, Substitute.For<ILogger>());
        
            var seedServers = peerSettings.ParseDnsServersFromConfig(_config);
        
            seedServers.Should().NotBeNullOrEmpty();
            seedServers.Should().Contain(dnsDomains);
        }
    }
}
