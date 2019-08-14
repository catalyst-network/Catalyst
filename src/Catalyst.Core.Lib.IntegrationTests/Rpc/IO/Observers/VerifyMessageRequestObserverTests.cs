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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Util;
using Catalyst.Core.Lib.Rpc.IO.Observers;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Network = Catalyst.Protocol.Common.Network;

namespace Catalyst.Core.Lib.IntegrationTests.Rpc.IO.Observers
{
    public sealed class VerifyMessageRequestObserverTests : ConfigFileBasedTest
    {
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;
        private readonly IKeySigner _keySigner;
        private readonly IChannelHandlerContext _fakeContext;

        public VerifyMessageRequestObserverTests(ITestOutputHelper output) : base(new[]
        {
            Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Common.Types.NetworkTypes.Dev)),
            Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile),
        }, output)
        {
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
        public async Task VerifyMessageRequest_UsingValidRequest_ShouldSendVerifyMessageResponse(string message, string signatureAndMessage, string publicKey, bool expectedResult)
        {
            var signatureMessageBytes = signatureAndMessage.HexToByteArray();
            ArraySegment<byte> signatureBytes = new ArraySegment<byte>(signatureMessageBytes, 0, FFI.SignatureLength);
            var publicKeyBytes = publicKey.HexToByteArray();
            var messageBytes = message.HexToByteArray();

            //results in an empty context, which is the only way we can use rfc test vectors to test the VerifyMessageRequestObserver. 
            var signingContext = new SigningContext
            {
                Network = Network.Unknown, SignatureType = SignatureType.Unknown
            };

            var messageFactory = new DtoFactory();

            var request = messageFactory.GetDto(
                new VerifyMessageRequest
                {
                    Message = RLP.EncodeElement(messageBytes).ToByteString(),
                    PublicKey = RLP.EncodeElement(publicKeyBytes).ToByteString(),
                    Signature = RLP.EncodeElement(signatureBytes.ToArray()).ToByteString(),
                    SigningContext = signingContext
                },
                PeerIdentifierHelper.GetPeerIdentifier("sender_key"),
                PeerIdentifierHelper.GetPeerIdentifier("recipient_key")
            );
            
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, 
                request.Content.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId)
            );
            
            var handler = new VerifyMessageRequestObserver(PeerIdentifierHelper.GetPeerIdentifier("sender"),
                _logger,
                _keySigner
            );

            handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments().Single();
            var verifyResponseMessage = sentResponseDto.FromIMessageDto().FromProtocolMessage<VerifyMessageResponse>();

            verifyResponseMessage.IsSignedByKey.Should().Be(expectedResult);
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
