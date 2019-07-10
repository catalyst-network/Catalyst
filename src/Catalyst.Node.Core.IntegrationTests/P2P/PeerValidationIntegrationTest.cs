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
using System.IO;
using System.Net;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.EventLoop;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.IO.Transport.Channels;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Serilog.Core;
using SharpRepository.InMemoryRepository;
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
        private readonly PeerClientFixture _peerClientFixture;

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
            //_peerClientFixture = new PeerClientFixture();

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

        [Fact(Skip = "due to major change")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void PeerChallenge_PeerIdentifiers_Expect_To_Succeed_Valid_IP_Port_PublicKey()
        {
            using (_container.BeginLifetimeScope(CurrentTestName))
            {
                using (var peerService = _container.Resolve<IPeerService>())
                {
                    var peerSettings = new PeerSettings(_config);
                    var targetHost = new IPEndPoint(peerSettings.BindAddress,
                        peerSettings.Port + new Random().Next(0, 5000));

                    var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
                    {
                        TcpClientHandlerWorkerThreads = 2,
                        TcpServerHandlerWorkerThreads = 3,
                        UdpServerHandlerWorkerThreads = 4,
                        UdpClientHandlerWorkerThreads = 5
                    };

                    var peerValidator = new PeerValidator(targetHost, peerSettings, peerService, _logger,
                        new PeerClient(new PeerClientChannelFactory(Substitute.For<IKeySigner>(),
                                Substitute.For<IMessageCorrelationManager>(), Substitute.For<IPeerIdValidator>()),
                            new UdpClientEventLoopGroupFactory(eventLoopGroupFactoryConfiguration)), new PeerIdentifier(peerSettings, Substitute.For<IPeerIdClientId>()));

                    var valid = peerValidator.PeerChallengeResponse(new PeerIdentifier(peerSettings, Substitute.For<IPeerIdClientId>()));

                    valid.Should().BeTrue();
                }
            }
        }

        [Theory(Skip = "due to major change")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [InlineData("Fr2a300k06032b657793", "92.207.178.198", 1574)]
        [InlineData("pp2a300k55032b657791", "198.51.100.3", 2524)]
        public void PeerChallenge_PeerIdentifiers_Expect_To_Fail_IP_Port_PublicKey(string publicKey, string ip, int port)
        {
            using (_container.BeginLifetimeScope(CurrentTestName))
            {
                using (var peerService = _container.Resolve<IPeerService>())
                {
                    var peerSettings = new PeerSettings(_config);
                    var targetHost = new IPEndPoint(peerSettings.BindAddress,
                        peerSettings.Port + new Random().Next(0, 5000));

                    var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
                    {
                        TcpClientHandlerWorkerThreads = 2,
                        TcpServerHandlerWorkerThreads = 3,
                        UdpServerHandlerWorkerThreads = 4,
                        UdpClientHandlerWorkerThreads = 5
                    };

                    var peerValidator = new PeerValidator(targetHost, peerSettings, peerService, _logger,
                        new PeerClient(new PeerClientChannelFactory(Substitute.For<IKeySigner>(),
                                Substitute.For<IMessageCorrelationManager>(), Substitute.For<IPeerIdValidator>()),
                            new UdpClientEventLoopGroupFactory(eventLoopGroupFactoryConfiguration)), new PeerIdentifier(peerSettings, Substitute.For<IPeerIdClientId>()));

                    var peerActiveId = new PeerIdentifier(publicKey.ToUtf8ByteString().ToByteArray(),
                        IPAddress.Parse(ip),
                        port, Substitute.For<IPeerIdClientId>());

                    var valid = peerValidator.PeerChallengeResponse(peerActiveId);

                    valid.Should().BeFalse();
                }
            }
        }
    }
}
