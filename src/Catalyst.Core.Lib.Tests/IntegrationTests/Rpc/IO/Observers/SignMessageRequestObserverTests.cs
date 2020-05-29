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
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.KeySigner;
using Catalyst.Core.Modules.Keystore;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Google.Protobuf;
using Catalyst.Core.Modules.Dfs;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.Rpc.IO.Observers
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public sealed class SignMessageRequestObserverTests : FileSystemBasedTest
    {
        private TestScheduler _testScheduler;
        private ILifetimeScope _scope;
        private ILogger _logger;
        private IKeySigner _keySigner;
        private IChannelHandlerContext _fakeContext;

        public SignMessageRequestObserverTests() : base(new[]
        {
            Path.Combine(Constants.ConfigSubFolder, TestConstants.TestShellNodesConfigFile)
        })
        {
  
        }


        [SetUp]
        public void Init()
        {
            Setup(TestContext.CurrentContext);
            _testScheduler = new TestScheduler();
            ContainerProvider.ContainerBuilder.RegisterInstance(TestKeyRegistry.MockKeyRegistry()).As<IKeyRegistry>();
            ContainerProvider.ContainerBuilder.RegisterModule(new KeystoreModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new KeySignerModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new BulletProofsModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new HashingModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new DfsModule());
            ContainerProvider.ConfigureContainerBuilder();

            _scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName);
            _keySigner = ContainerProvider.Container.Resolve<IKeySigner>();
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        [TestCase("Hello Catalyst")]
        [TestCase("")]
        [TestCase("Hello&?!1253Catalyst")]
       
        public async Task RpcServer_Can_Handle_SignMessageRequest(string message)
        {
            var sender = PeerIdHelper.GetPeerId("sender");
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.Address.Returns(sender);
            var signMessageRequest = new SignMessageRequest
            {
                Message = message.ToUtf8ByteString(),
                SigningContext = new SigningContext()
            };
            var protocolMessage =
                signMessageRequest.ToProtocolMessage(sender);

            await TaskHelper.WaitForAsync(
                () => _keySigner.GetPrivateKey(KeyRegistryTypes.DefaultKey) != null, 
                TimeSpan.FromSeconds(2));

            var messageStream =
                MessageStreamHelper.CreateStreamWithMessage(_fakeContext, _testScheduler, protocolMessage);
            var handler =
                new SignMessageRequestObserver(peerSettings, _logger, _keySigner);

            handler.StartObserving(messageStream);

            _testScheduler.Start();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            _logger.DidNotReceiveWithAnyArgs().Error((Exception) default, default);
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments().Single();
            var signResponseMessage = sentResponseDto.Content.FromProtocolMessage<SignMessageResponse>();

            signResponseMessage.OriginalMessage.Should().Equal(ByteString.CopyFromUtf8(message));
            signResponseMessage.Signature.Should().NotBeEmpty();
            signResponseMessage.PublicKey.Should().NotBeEmpty();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scope?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
