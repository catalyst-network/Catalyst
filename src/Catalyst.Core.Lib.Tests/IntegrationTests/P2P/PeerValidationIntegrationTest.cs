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
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P.Protocols;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Cli;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.EventLoop;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Lib.P2P.IO.Transport.Channels;
using Catalyst.Core.Lib.P2P.Protocols;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.KeySigner;
using Catalyst.Core.Modules.Keystore;
using Catalyst.TestUtils;
using FluentAssertions;
using Lib.P2P;
using MultiFormats;
using NSubstitute;
using NUnit.Framework;
using Serilog;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.P2P
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public sealed class PeerValidationIntegrationTest : FileSystemBasedTest
    {
        private IPeerService _peerService;
        private IPeerChallengeRequest _peerChallengeRequest;
        private PeerSettings _peerSettings;

        [SetUp]
        public void Init()
        {
            Setup(TestContext.CurrentContext);

            var logger = Substitute.For<ILogger>();

            var keyRegistry = TestKeyRegistry.MockKeyRegistry();
            ContainerProvider.ContainerBuilder.RegisterInstance(keyRegistry).As<IKeyRegistry>();

            ContainerProvider.ContainerBuilder.RegisterModule(new CoreLibProvider());
            ContainerProvider.ContainerBuilder.RegisterModule(new KeystoreModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new KeySignerModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new HashingModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new BulletProofsModule());

            ContainerProvider.ContainerBuilder.RegisterType<ConsoleUserOutput>().As<IUserOutput>();
            ContainerProvider.ContainerBuilder.RegisterType<ConsoleUserInput>().As<IUserInput>();

            var passwordManager = Substitute.For<IPasswordManager>();
            passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(Arg.Is(PasswordRegistryTypes.DefaultNodePassword), Arg.Any<string>()).Returns(TestPasswordReader.BuildSecureStringPassword("test"));
            ContainerProvider.ContainerBuilder.RegisterInstance(passwordManager).As<IPasswordManager>();

            _peerSettings = (PeerSettings) ContainerProvider.Container.Resolve<IPeerSettings>();

            ContainerProvider.ContainerBuilder.Register(c =>
            {
                var peerClient = c.Resolve<ILibP2PPeerClient>();
                peerClient.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                return new PeerChallengeRequest(logger, peerClient, _peerSettings, 10);
            }).As<IPeerChallengeRequest>().SingleInstance();

            
            _peerChallengeRequest = ContainerProvider.Container.Resolve<IPeerChallengeRequest>();

            var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
            {
                TcpClientHandlerWorkerThreads = 2,
                TcpServerHandlerWorkerThreads = 3,
                UdpServerHandlerWorkerThreads = 4,
                UdpClientHandlerWorkerThreads = 5
            };

            var keySigner = ContainerProvider.Container.Resolve<IKeySigner>(); // @@

            _peerService = new PeerService(
                new UdpServerEventLoopGroupFactory(eventLoopGroupFactoryConfiguration),
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

            _peerService.StartAsync().Wait();
        }

        // [Fact(Skip = "this wont work as it tries to connect to a real node!! We need to instantiate two sockets here")]
        // public async Task PeerChallenge_PeerIdentifiers_Expect_To_Succeed_Valid_IP_Port_PublicKey()
        // {
        //     // await Setup().ConfigureAwait(false);
        //     // var valid = await RunPeerChallengeTask(_peerSettings.PublicKey, _peerSettings.BindAddress,
        //     //     _peerSettings.Port).ConfigureAwait(false);
        //     //
        //     // valid.Should().BeTrue();
        // }

        [Theory]
        [TestCase("42Bm3dA9xKjnMYp5CztnUYqEme4Y46suNCvKx6ueDhz", "92.207.178.198", 1574)]
        [TestCase("483NWsFHJ5UrTRf8q6Ew3E8mD89i9cXyaxxFM1sSn4q1", "198.51.100.3", 2524)]
        public async Task PeerChallenge_PeerIdentifiers_Expect_To_Fail_IP_Port_PublicKey(string publicKey,
            string ip,
            int port)
        {
            var valid = await RunPeerChallengeTask(publicKey, IPAddress.Parse(ip), port).ConfigureAwait(false);

            valid.Should().BeFalse();
        }

        private async Task<bool> RunPeerChallengeTask(string publicKey, IPAddress ip, int port)
        {
            TestContext.WriteLine(publicKey);
            TestContext.WriteLine(ip.ToString());
            TestContext.WriteLine(port.ToString());

            //todo
            //var recipient = publicKey.BuildPeerIdFromBase58Key(ip, port);

            var recipient = PeerIdHelper.GetPeerId(publicKey, ip, port);
            return await _peerChallengeRequest.ChallengePeerAsync(recipient);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(true);
            _peerService?.Dispose();
        }
    }
}
