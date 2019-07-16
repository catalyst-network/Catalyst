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

using System.IO;
using System.Net;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.EventLoop;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.IO.Transport.Channels;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Common.IO.Messaging.Correlation;
using Constants = Catalyst.Common.Config.Constants;
using Catalyst.Node.Core.P2P.IO.Messaging.Broadcast;

namespace Catalyst.Node.Core.IntegrationTests.P2P
{
    public sealed class PeerValidationIntegrationTest : ConfigFileBasedTest
    {
        private readonly ILogger _logger;
        private readonly IContainer _container;
        private readonly IConfigurationRoot _config;
        private readonly IKeySigner _serverKeySigner;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly EventLoopGroupFactoryConfiguration _eventLoopGroupFactoryConfiguration;

        public PeerValidationIntegrationTest(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Test)))
               .Build(), CurrentTestName);

            _logger = Substitute.For<ILogger>();
            _serverKeySigner = Substitute.For<IKeySigner>();
            _peerIdValidator = Substitute.For<IPeerIdValidator>();

            var sigBytes = ByteUtil.GenerateRandomByteArray(Cryptography.BulletProofs.Wrapper.FFI.GetSignatureLength());
            var pubBytes = ByteUtil.GenerateRandomByteArray(Cryptography.BulletProofs.Wrapper.FFI.GetPublicKeyLength());
            var sig = new Signature(sigBytes, pubBytes);

            _serverKeySigner.Sign(Arg.Any<byte[]>()).ReturnsForAnyArgs(sig);


            _eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
            {
                TcpClientHandlerWorkerThreads = 2,
                TcpServerHandlerWorkerThreads = 3,
                UdpServerHandlerWorkerThreads = 4,
                UdpClientHandlerWorkerThreads = 5
            };


            ConfigureContainerBuilder(_config, true, true);
            ContainerBuilder.RegisterType<MessageCorrelationManager>().SingleInstance();

            _container = ContainerBuilder.Build();

            //var builder = ContainerBuilder;
            //builder.RegisterType<IMessageCorrelationManager>().SingleInstance();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void PeerChallenge_PeerIdentifiers_Expect_To_Succeed_Valid_IP_Port_PublicKey()
        {
            using (_container.BeginLifetimeScope(CurrentTestName))
            {
                using (var peerService = _container.Resolve<IPeerService>())
                {
                    var peerSettings = new PeerSettings(_config);

                    var recipient = new PeerIdentifier(peerSettings.PublicKey.ToUtf8ByteString().ToByteArray(), peerSettings.BindAddress,
                        peerSettings.Port, Substitute.For<IPeerIdClientId>());

                    var sender = PeerIdentifierHelper.GetPeerIdentifier("sender", "Tc", 1, peerSettings.BindAddress, peerSettings.Port);

                    _peerIdValidator.ValidatePeerIdFormat(sender.PeerId);
                    _peerIdValidator.ValidatePeerIdFormat(recipient.PeerId);

                    var peerServerFact = ((PeerServerChannelFactory)((PeerService)peerService).ChannelFactory);
                    var ch = peerServerFact._messageCorrelationManager;
                    var boardcastMgr = peerServerFact._broadcastManager;
                    var peerClient = ((BroadcastManager)boardcastMgr)._peerClient;

                    var peerValidator = new PeerValidator(peerSettings, peerService, _logger, peerClient, sender);


                    //var peerValidator = new PeerValidator(peerSettings, peerService, _logger,
                    //    new PeerClient(new PeerClientChannelFactory(_serverKeySigner,
                    //           ch, _peerIdValidator),
                    //        new UdpClientEventLoopGroupFactory(_eventLoopGroupFactoryConfiguration)), sender);

                    var valid = peerValidator.PeerChallengeResponse(recipient);

                    valid.Should().BeTrue();
                }
            }
        }

        [Theory]
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

                    var recipient = new PeerIdentifier(publicKey.ToUtf8ByteString().ToByteArray(), IPAddress.Parse(ip),
                        port, Substitute.For<IPeerIdClientId>());

                    var sender = PeerIdentifierHelper.GetPeerIdentifier("sender", "Tc", 1, peerSettings.BindAddress, peerSettings.Port);

                    _peerIdValidator.ValidatePeerIdFormat(sender.PeerId);
                    _peerIdValidator.ValidatePeerIdFormat(recipient.PeerId);

                    var peerValidator = new PeerValidator(peerSettings, peerService, _logger,
                        new PeerClient(new PeerClientChannelFactory(_serverKeySigner,
                                Substitute.For<IMessageCorrelationManager>(), _peerIdValidator),
                            new UdpClientEventLoopGroupFactory(_eventLoopGroupFactoryConfiguration)), sender);

                    var valid = peerValidator.PeerChallengeResponse(recipient);

                    valid.Should().BeFalse();
                }
            }
        }
    }
}
