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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.Network;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Protocol.IPPN;
using DnsClient;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Serilog.Core;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;
using Xunit;
using Xunit.Abstractions;
using Constants = Catalyst.Common.Config.Constants;
using Peer = Catalyst.Common.P2P.Peer;

namespace Catalyst.Node.Core.UnitTests.P2P
{
    public sealed class PeerDiscoveryUnitTest : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;
        private readonly IDns _dns;
        private readonly IRepository<Peer> _peerRepository;
        private readonly ILogger _logger;
        private readonly ILookupClient _lookupClient;
        private readonly List<string> _dnsDomains;
        private readonly string _seedPid;

        public PeerDiscoveryUnitTest(ITestOutputHelper output) : base(output)
        {
            _peerRepository = Substitute.For<IRepository<Peer>>();
            _logger = Substitute.For<ILogger>();
            _lookupClient = Substitute.For<ILookupClient>();
            _dns = new Common.Network.DnsClient(_lookupClient);

            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
            
            _dnsDomains = new List<string>
            {
                "seed1.catalystnetwork.io",
                "seed2.catalystnetwork.io",
                "seed3.catalystnetwork.io",
                "seed4.catalystnetwork.io",
                "seed5.catalystnetwork.io"
            };
            
            _seedPid = "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839";
        }

        [Fact]
        public void ResolvesIPeerDiscoveryCorrectly()
        {
            ConfigureContainerBuilder(_config);

            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var peerDiscovery = container.Resolve<IPeerDiscovery>();
                Assert.NotNull(peerDiscovery);
                peerDiscovery.Should().BeOfType(typeof(PeerDiscovery));
                Assert.NotNull(peerDiscovery.Dns);
                peerDiscovery.Dns.Should().BeOfType(typeof(DevDnsClient));
                Assert.NotNull(peerDiscovery.Logger);
                peerDiscovery.Logger.Should().BeOfType(typeof(Logger));
                Assert.NotNull(peerDiscovery.Peers);
                peerDiscovery.Peers.Should().BeOfType(typeof(ConcurrentQueue<IPeerIdentifier>));
                Assert.NotNull(peerDiscovery.PeerRepository);
                peerDiscovery.PeerRepository.Should().BeOfType(typeof(InMemoryRepository<Peer>));
            }
        }

        [Fact]
        public void CanParseDnsNodesFromConfig()
        {
            _dnsDomains.ForEach(domain =>
            {
                MockQueryResponse.CreateFakeLookupResult(domain, _seedPid, _lookupClient);
            });
            
            var peerDiscovery = new PeerDiscovery(_dns, _peerRepository, _config, _logger);

            var seedServers = peerDiscovery.ParseDnsServersFromConfig(_config);

            seedServers.Should().NotBeNullOrEmpty();
            seedServers.Should().Contain(_dnsDomains);
        }

        [Fact]
        public void CanGetSeedNodesFromDns()
        {
            _dnsDomains.ForEach(domain =>
            {
                MockQueryResponse.CreateFakeLookupResult(domain, _seedPid, _lookupClient);
            });

            var peerDiscovery = new PeerDiscovery(_dns, _peerRepository, _config, _logger);

            peerDiscovery.Peers.Should().NotBeNullOrEmpty();
            peerDiscovery.Peers.Should().HaveCount(1);
            peerDiscovery.Peers.Should().NotContainNulls();
            peerDiscovery.Peers.Should().ContainItemsAssignableTo<IPeerIdentifier>();
        }

        [Fact]
        public void CanReceivePingEventsFromSubscribedStream()
        {
            _dnsDomains.ForEach(domain =>
            {
                MockQueryResponse.CreateFakeLookupResult(domain, _seedPid, _lookupClient);
            });
            
            var peerDiscovery = new PeerDiscovery(_dns, _peerRepository, _config, _logger);

            var fakeContext = Substitute.For<IChannelHandlerContext>();
            var pingRequest = new PingResponse();
            var pid = PeerIdentifierHelper.GetPeerIdentifier("im_a_key");
            var channeledAny = new ChanneledAnySigned(fakeContext, 
                pingRequest.ToAnySigned(pid.PeerId, Guid.NewGuid()));
            
            var observableStream = new[] {channeledAny}.ToObservable();

            peerDiscovery.StartObserving(observableStream);

            _peerRepository.Received(1)
               .Add(Arg.Is<Peer>(p => p.PeerIdentifier.Equals(pid)));
        }
    }
}
