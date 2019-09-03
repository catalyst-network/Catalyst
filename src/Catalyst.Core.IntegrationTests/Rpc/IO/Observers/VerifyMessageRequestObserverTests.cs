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
using Catalyst.Core.Config;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Core.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.IntegrationTests.Rpc.IO.Observers
{
    public sealed class VerifyMessageRequestObserverTests : ConfigFileBasedTest
    {
        private readonly TestScheduler _testScheduler;
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;
        private readonly IKeySigner _keySigner;
        private readonly IChannelHandlerContext _fakeContext;

        public VerifyMessageRequestObserverTests(ITestOutputHelper output) : base(new[]
        {
            Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Protocol.Common.Network.Devnet)),
            Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile)
        }, output)
        {
            _testScheduler = new TestScheduler();
            ContainerProvider.ConfigureContainerBuilder();

            SocketPortHelper.AlterConfigurationToGetUniquePort(ContainerProvider.ConfigurationRoot, CurrentTestName);

            _scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName);
            
            _keySigner = _scope.Resolve<IKeySigner>();
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        //From https://tools.ietf.org/html/rfc8032#section-7.3
        [Theory]
        [InlineData("616263", "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406", "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", true)]
        [InlineData("616263", "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083403", "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", false)]
#pragma warning disable 1998
        public 
            async Task VerifyMessageRequestObserver_Should_Send_Correct_Response(string message, string signatureAndMessage, string publicKey, bool expectedResult)
#pragma warning restore 1998
        {
            var sender = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var signatureMessageBytes = signatureAndMessage.HexToByteArray();
            ArraySegment<byte> signatureBytes = new ArraySegment<byte>(signatureMessageBytes, 0, FFI.SignatureLength);
            var publicKeyBytes = publicKey.HexToByteArray();
            var messageBytes = message.HexToByteArray();

            //results in an empty context, which is the only way we can use rfc test vectors to test the VerifyMessageRequestObserver. 
            var signingContext = new SigningContext
            {
                Network = Protocol.Common.Network.Unknown, SignatureType = SignatureType.Unknown
            };

            var verifyMessageRequest = new VerifyMessageRequest
            {
                Message = RLP.EncodeElement(messageBytes).ToByteString(),
                PublicKey = RLP.EncodeElement(publicKeyBytes).ToByteString(),
                Signature = RLP.EncodeElement(signatureBytes.ToArray()).ToByteString(),
                SigningContext = signingContext
            };
            var protocolMessage =
                verifyMessageRequest.ToProtocolMessage(sender.PeerId);

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, _testScheduler,
                protocolMessage
            );
            
            var handler = new VerifyMessageRequestObserver(sender,
                _logger,
                _keySigner
            );

            handler.StartObserving(messageStream);

            _testScheduler.Start();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments().Single();
            var verifyResponseMessage = sentResponseDto.Content.FromProtocolMessage<VerifyMessageResponse>();

            verifyResponseMessage.IsSignedByKey.Should().Be(expectedResult);
        }

        [Fact]
#pragma warning disable 1998
        public async Task VerifyMessageRequest_Can_Verify_Valid_SignMessageResponse()
#pragma warning restore 1998
        {
            var sender = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var signingContext = new SigningContext
            {
                Network = Protocol.Common.Network.Devnet, SignatureType = SignatureType.ProtocolRpc
            };

            var message = "something something something";

            var signMessageRequest = new SignMessageRequest
            {
                Message = message.ToUtf8ByteString(),
                SigningContext = signingContext
            };
            var protocolMessage =
                signMessageRequest.ToProtocolMessage(sender.PeerId);

            var signRequest = new MessageDto(protocolMessage,
                PeerIdentifierHelper.GetPeerIdentifier("recipient_key")
            );

            var signMessageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, _testScheduler, signRequest.Content);
            var signHandler = new SignMessageRequestObserver(sender, _logger, _keySigner);

            signHandler.StartObserving(signMessageStream);

            _testScheduler.Start();

            var receivedCallsSign = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCallsSign.Count.Should().Be(1);
            
            var sentSignResponseDto = (IMessageDto<ProtocolMessage>) receivedCallsSign.Single().GetArguments().Single();
            var signResponseMessage = sentSignResponseDto.Content.FromProtocolMessage<SignMessageResponse>();

            signResponseMessage.OriginalMessage.Should().Equal(message);
            signResponseMessage.Signature.Should().NotBeEmpty();
            signResponseMessage.PublicKey.Should().NotBeEmpty();

            _fakeContext.Channel.ClearReceivedCalls();

            var verifyMessageRequest = new VerifyMessageRequest
            {
                Message = RLP.EncodeElement(signResponseMessage.OriginalMessage.ToByteArray()).ToByteString(),
                PublicKey = RLP.EncodeElement(signResponseMessage.PublicKey.ToByteArray()).ToByteString(),
                Signature = RLP.EncodeElement(signResponseMessage.Signature.ToByteArray()).ToByteString(),
                SigningContext = signingContext
            };

            var verifyMessageRequestProtocolMessage =
                verifyMessageRequest.ToProtocolMessage(sender.PeerId);

            var verifyMessageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, _testScheduler,
                verifyMessageRequestProtocolMessage
            );
            
            var verifyHandler = new VerifyMessageRequestObserver(sender,
                _logger,
                _keySigner
            );

            verifyHandler.StartObserving(verifyMessageStream);

            _testScheduler.Start();

            var receivedCallsVerify = _fakeContext.Channel.ReceivedCalls().ToList();

            var sentVerifyResponseDto = (IMessageDto<ProtocolMessage>) receivedCallsVerify.Single().GetArguments().Single();
            var verifyResponseMessage = sentVerifyResponseDto.Content.FromProtocolMessage<VerifyMessageResponse>();

            verifyResponseMessage.IsSignedByKey.Should().Be(true);
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
