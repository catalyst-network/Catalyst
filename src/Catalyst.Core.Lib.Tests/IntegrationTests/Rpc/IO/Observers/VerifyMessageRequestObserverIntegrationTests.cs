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

using Autofac;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Types;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using System.Linq;
using Catalyst.Abstractions.Keystore;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Modules.Consensus;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Dfs;
using Catalyst.Core.Modules.KeySigner;
using Catalyst.Core.Modules.Keystore;
using Catalyst.Core.Modules.Rpc.Server;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using NUnit.Framework;
using Catalyst.Core.Modules.Mempool;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Peer;
using Catalyst.Core.Modules.Authentication;
using Catalyst.Core.Modules.Hashing;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.Rpc.IO.Observers
{
    public class VerifyMessageRequestObserverIntegrationTests : FileSystemBasedTest
    {
        private IKeySigner _keySigner;
        private IChannelHandlerContext _fakeContext;
        private IRpcRequestObserver _verifyMessageRequestObserver;
        private ILifetimeScope _scope;
        private PeerId _peerId;
        private ByteString _testMessageToSign;
        
        public VerifyMessageRequestObserverIntegrationTests() : base(TestContext.CurrentContext)
        {
        }

        [SetUp]
        public void Init()
        {
            Setup(TestContext.CurrentContext);

            _testMessageToSign = ByteString.CopyFromUtf8("TestMsg");

            ContainerProvider.ContainerBuilder.RegisterInstance(TestKeyRegistry.MockKeyRegistry()).As<IKeyRegistry>();
            ContainerProvider.ContainerBuilder.RegisterModule(new KeystoreModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new KeySignerModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new RpcServerModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new DfsModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new MempoolModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new ConsensusModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new BulletProofsModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new AuthenticationModule());
            ContainerProvider.ContainerBuilder.RegisterModule(new HashingModule());
            ContainerProvider.ContainerBuilder.RegisterType<VerifyMessageRequestObserver>().As<IRpcRequestObserver>();

            ContainerProvider.ContainerBuilder.RegisterInstance(PeerIdHelper.GetPeerId("Test"))
               .As<PeerId>();

            ContainerProvider.ConfigureContainerBuilder();

            _scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName);
            _keySigner = ContainerProvider.Container.Resolve<IKeySigner>();
            _peerId = ContainerProvider.Container.Resolve<PeerId>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();

            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
            _verifyMessageRequestObserver = _scope.Resolve<IRpcRequestObserver>();
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void Valid_Message_Signature_Can_Return_True_Response()
        {
            var privateKey = _keySigner.KeyStore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey);

            var signingContext = new SigningContext
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };

            var requestMessage = new VerifyMessageRequest
            {
                Message = _testMessageToSign,
                PublicKey = privateKey.GetPublicKey().Bytes.ToByteString(),
                Signature = _keySigner.CryptoContext.Sign(privateKey, _testMessageToSign.ToByteArray(), signingContext.ToByteArray()).SignatureBytes.ToByteString(),
                SigningContext = signingContext
            };

            _verifyMessageRequestObserver
               .OnNext(new ObserverDto(_fakeContext,
                    requestMessage.ToProtocolMessage(_peerId)));
            AssertVerifyResponse(true);
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void Invalid_Message_Signature_Can_Return_False_Response()
        {
            var requestMessage = new VerifyMessageRequest
            {
                Message = _testMessageToSign,
                PublicKey = ByteString.CopyFrom(new byte[_keySigner.CryptoContext.PublicKeyLength]),
                Signature = ByteString.CopyFrom(new byte[_keySigner.CryptoContext.SignatureLength]),
                SigningContext = new SigningContext()
            };

            _verifyMessageRequestObserver
               .OnNext(new ObserverDto(_fakeContext,
                    requestMessage.ToProtocolMessage(_peerId)));
            AssertVerifyResponse(false);
        }

        private void AssertVerifyResponse(bool valid)
        {
            var responseList = _fakeContext.Channel.ReceivedCalls().ToList();
            var response = ((MessageDto) responseList[0].GetArguments()[0]).Content
               .FromProtocolMessage<VerifyMessageResponse>();
            response.IsSignedByKey.Should().Be(valid);
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
