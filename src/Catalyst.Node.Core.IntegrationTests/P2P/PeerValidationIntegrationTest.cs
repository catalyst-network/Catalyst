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
using System.Net;
using System.Reactive.Linq;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.P2P;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
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

namespace Catalyst.Node.Core.IntegrationTests.P2P
{
    public sealed class PeerValidationIntegrationTest : ConfigFileBasedTest
    {
        private readonly Guid _guid;
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _pid;
        private readonly IContainer _container;
        private readonly PingRequest _pingRequest;
        private readonly IConfigurationRoot _config;
        public PeerValidationIntegrationTest(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Test)))
               .Build(), CurrentTestName);
            _pid = PeerIdentifierHelper.GetPeerIdentifier("im_a_key");
            _guid = Guid.NewGuid();
            _logger = Substitute.For<ILogger>();
            _pingRequest = new PingRequest();

            ConfigureContainerBuilder(_config, true, true);

            _container = ContainerBuilder.Build();
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

        [Theory(Skip = "due to peer service refactor")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [InlineData("Fr2a300k06032b657793", "92.207.178.198", 1574)]
        [InlineData("pp2a300k55032b657791", "198.51.100.3", 2524)]
        public void PeerChallenge_PeerIdentifiers_Expect_To_Fail_IP_Port_PublicKey(string publicKey, string ip, int port)
        {
            using (_container.BeginLifetimeScope(CurrentTestName))
            {
            }
        }

        //[Fact]
        //public void CanReceivePingEventsFromSubscribedStream()
        //{
        //    _dnsDomains.ForEach(domain =>
        //    {
        //        MockQueryResponse.CreateFakeLookupResult(domain, _seedPid, _lookupClient);
        //    });
            
        //    var peerDiscovery = new PeerDiscovery(_dns, _peerRepository, _config, _logger);

        //    var fakeContext = Substitute.For<IChannelHandlerContext>();
        //    var pingRequest = new PingResponse();
        //    var pid = PeerIdentifierHelper.GetPeerIdentifier("im_a_key");
        //    var channeledAny = new ObserverDto(fakeContext, 
        //        pingRequest.ToProtocolMessage(pid.PeerId, CorrelationId.GenerateCorrelationId()));
            
        //    var observableStream = new[] {channeledAny}.ToObservable();

        //    peerDiscovery.StartObserving(observableStream);

        //    _peerRepository.Received(1)
        //       .Add(Arg.Is<Peer>(p => p.PeerIdentifier.Equals(pid)));
        //}
    }
}
