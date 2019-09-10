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
using Catalyst.Core.Config;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Extensions;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using System.IO;
using System.Linq;
using Catalyst.Abstractions.P2P;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.IntegrationTests.Rpc.IO.Observers
{
    public class VerifyMessageRequestObserverIntegrationTests : ConfigFileBasedTest
    {
        private readonly IKeySigner _keySigner;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IRpcRequestObserver _verifyMessageRequestObserver;
        private readonly ILifetimeScope _scope;
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly ByteString _testMessageToSign;
        
        public VerifyMessageRequestObserverIntegrationTests(ITestOutputHelper output) : base(output, new[]
        {
            Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Protocol.Common.Network.Devnet)),
            Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile)
        })
        {
            _testMessageToSign = ByteString.CopyFromUtf8("TestMsg");

            ContainerProvider.ConfigureContainerBuilder();
            SocketPortHelper.AlterConfigurationToGetUniquePort(ContainerProvider.ConfigurationRoot, CurrentTestName);

            _scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName);
            _keySigner = ContainerProvider.Container.Resolve<IKeySigner>();
            _peerIdentifier = ContainerProvider.Container.Resolve<IPeerIdentifier>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);

            var observer = _scope.Resolve<IRpcRequestObserver[]>();
            _verifyMessageRequestObserver = observer.Single(t => t is VerifyMessageRequestObserver);
        }

        [Fact]
        public void Valid_Message_Signature_Can_Return_True_Response()
        {
            var privateKey = _keySigner.KeyStore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey);

            var signingContext = new SigningContext
            {
                Network = Protocol.Common.Network.Devnet,
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
                    requestMessage.ToProtocolMessage(_peerIdentifier.PeerId)));
            AssertVerifyResponse(true);
        }

        [Fact]
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
                    requestMessage.ToProtocolMessage(_peerIdentifier.PeerId)));
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
