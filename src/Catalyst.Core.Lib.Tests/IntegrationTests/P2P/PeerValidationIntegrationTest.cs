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
using System.Net;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.EventLoop;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Lib.P2P.IO.Transport.Channels;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.KeySigner;
using Catalyst.Core.Modules.Keystore;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.P2P
{
    public sealed class PeerValidationIntegrationTest : FileSystemBasedTest
    {
        private IPeerService _peerService;
        private IPeerChallenger _peerChallenger;
        private readonly PeerSettings _peerSettings;

        public PeerValidationIntegrationTest(ITestOutputHelper output) : base(output)
        {
            _peerSettings = new PeerSettings(ContainerProvider.ConfigurationRoot);

            var peerSettings =
                PeerIdHelper.GetPeerId("sender", _peerSettings.BindAddress, _peerSettings.Port).ToSubstitutedPeerSettings();

            var logger = Substitute.For<ILogger>();
            var keyRegistry = TestKeyRegistry.MockKeyRegistry();
            
            ContainerProvider.ContainerBuilder.RegisterInstance(keyRegistry).As<IKeyRegistry>();
            ContainerProvider.ContainerBuilder.RegisterModule(new KeystoreModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new KeySignerModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new BulletProofsModule());
            ContainerProvider.ContainerBuilder.Register(c =>
            {
                var peerClient = c.Resolve<IPeerClient>();
                peerClient.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                return new PeerChallenger(logger, peerClient, peerSettings, 5);
            }).As<IPeerChallenger>().SingleInstance();
        }

        private async Task Setup()
        {
            _peerChallenger = ContainerProvider.Container.Resolve<IPeerChallenger>();

            var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
            {
                TcpClientHandlerWorkerThreads = 2,
                TcpServerHandlerWorkerThreads = 3,
                UdpServerHandlerWorkerThreads = 4,
                UdpClientHandlerWorkerThreads = 5
            };

            var keySigner = Substitute.For<IKeySigner>();
            keySigner.Verify(Arg.Any<ISignature>(), Arg.Any<byte[]>(), default).ReturnsForAnyArgs(true);
            var signature = Substitute.For<ISignature>();
            keySigner.Sign(Arg.Any<byte[]>(), default).ReturnsForAnyArgs(signature);

            _peerService = new PeerService(new UdpServerEventLoopGroupFactory(eventLoopGroupFactoryConfiguration),
                new PeerServerChannelFactory(ContainerProvider.Container.Resolve<IPeerMessageCorrelationManager>(),
                    ContainerProvider.Container.Resolve<IBroadcastManager>(),
                    keySigner,
                    ContainerProvider.Container.Resolve<IPeerIdValidator>(),
                    ContainerProvider.Container.Resolve<IPeerSettings>()),
                new DiscoveryHelper.DevDiscover(), 
                ContainerProvider.Container.Resolve<IEnumerable<IP2PMessageObserver>>(),
                _peerSettings,
                ContainerProvider.Container.Resolve<ILogger>(),
                ContainerProvider.Container.Resolve<IHealthChecker>());

            await _peerService.StartAsync();
        }

        [Fact(Skip = "false")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task PeerChallenge_PeerIdentifiers_Expect_To_Succeed_Valid_IP_Port_PublicKey()
        {
            await Setup().ConfigureAwait(false);
            var valid = await RunPeerChallengeTask(_peerSettings.PublicKey, _peerSettings.BindAddress,
                _peerSettings.Port).ConfigureAwait(false);

            valid.Should().BeTrue();
        }

        [Theory]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [InlineData("Fr2a300k06032b657793", "92.207.178.198", 1574)]
        [InlineData("pp2a300k55032b657791", "198.51.100.3", 2524)]
        public async Task PeerChallenge_PeerIdentifiers_Expect_To_Fail_IP_Port_PublicKey(string publicKey,
            string ip,
            int port)
        {
            await Setup().ConfigureAwait(false);
            var valid = await RunPeerChallengeTask(publicKey, IPAddress.Parse(ip), port).ConfigureAwait(false);

            valid.Should().BeFalse();
        }

        private async Task<bool> RunPeerChallengeTask(string publicKey, IPAddress ip, int port)
        {
            Output.WriteLine(publicKey);
            Output.WriteLine(ip.ToString());
            Output.WriteLine(port.ToString());

            var recipient = publicKey.BuildPeerIdFromBase32CrockfordKey(ip, port);
            
            return await _peerChallenger.ChallengePeerAsync(recipient);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(true);
            _peerService?.Dispose();
        }
    }
}
