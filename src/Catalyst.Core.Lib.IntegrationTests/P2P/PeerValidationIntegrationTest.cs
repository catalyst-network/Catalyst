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
using System.IO;
using System.Net;
using System.Text;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Modules.KeySigner;
using Catalyst.Common.P2P;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Catalyst.Common.IO.EventLoop;
using Catalyst.Core.Lib.P2P.IO.Transport.Channels;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Core.Lib.P2P;
using System.Collections.Generic;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.Keystore;
using System.Threading.Tasks;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Constants = Catalyst.Common.Config.Constants;

namespace Catalyst.Core.Lib.IntegrationTests.P2P
{
    public sealed class PeerValidationIntegrationTest : ConfigFileBasedTest, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IContainer _container;
        private readonly IConfigurationRoot _config;
        private IPeerService _peerService;
        private PeerSettings _peerSettings;
        private IPeerClient _peerClientSingleInstance;
        public class TemporaryKeySigner : KeySigner
        {
            public TemporaryKeySigner(IKeyStore keyStore,
                     ICryptoContext cryptoContext)
                : base(keyStore, cryptoContext)
            {

            }
            public override bool Verify(ISignature signature, byte[] message)
            {
                return true;
            }
        }

        public PeerValidationIntegrationTest(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Test)))
               .Build(), CurrentTestName);

            _logger = Substitute.For<ILogger>();

            ConfigureContainerBuilder(_config, true, true);
            ContainerBuilder.RegisterType<KeySigner>().SingleInstance();

            _container = ContainerBuilder.Build();

             Setup();
        }

        private void Setup()
        {
            _peerSettings = new PeerSettings(_config);

            _peerClientSingleInstance = _container.Resolve<IPeerClient>();

            var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
            {
                TcpClientHandlerWorkerThreads = 2,
                TcpServerHandlerWorkerThreads = 3,
                UdpServerHandlerWorkerThreads = 4,
                UdpClientHandlerWorkerThreads = 5
            };

            var keysStore = new TemporaryKeySigner(_container.Resolve<IKeyStore>(), _container.Resolve<ICryptoContext>());

            _peerService = new PeerService(new UdpServerEventLoopGroupFactory(eventLoopGroupFactoryConfiguration),
                  new PeerServerChannelFactory(_container.Resolve<IPeerMessageCorrelationManager>(),
                  _container.Resolve<IBroadcastManager>(),
                   keysStore,
                   _container.Resolve<IPeerIdValidator>()), _container.Resolve<IPeerDiscovery>(),
                   _container.Resolve<IEnumerable<IP2PMessageObserver>>(), _container.Resolve<IPeerSettings>(), _container.Resolve<ILogger>());
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        async public Task PeerChallenge_PeerIdentifiers_Expect_To_Succeed_Valid_IP_Port_PublicKey()
        {
            var valid = await RunPeerChallengeTask(_peerSettings.PublicKey, _peerSettings.BindAddress, _peerSettings.Port).ConfigureAwait(false); 

            valid.Should().BeTrue();
        }

        [Theory]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [InlineData("Fr2a300k06032b657793", "92.207.178.198", 1574)]
        [InlineData("pp2a300k55032b657791", "198.51.100.3", 2524)]
        async public Task PeerChallenge_PeerIdentifiers_Expect_To_Fail_IP_Port_PublicKey(string publicKey, string ip, int port)
        {
            var valid = await RunPeerChallengeTask(publicKey, IPAddress.Parse(ip), port).ConfigureAwait(false);

            valid.Should().BeFalse();
        }

       async private Task<bool> RunPeerChallengeTask(string publicKey, IPAddress ip, int port)
        {
            var recipient = new PeerIdentifier(Encoding.UTF8.GetBytes(publicKey), ip,
                port, Substitute.For<IPeerIdClientId>());

            var sender = PeerIdentifierHelper.GetPeerIdentifier("sender", "Tc", 1, _peerSettings.BindAddress, _peerSettings.Port);

            var peerValidator = new PeerChallenger(_peerSettings, _peerService, _logger, _peerClientSingleInstance, sender);

            return await peerValidator.ChallengePeerAsync(recipient);
        }

        public new void Dispose()
        {
            base.Dispose();
            _peerService?.Dispose();
        }
    }   
}
