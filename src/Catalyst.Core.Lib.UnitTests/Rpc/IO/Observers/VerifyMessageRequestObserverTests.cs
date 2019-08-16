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

using System.Linq;
using System.Threading.Tasks;
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
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.Rpc.IO.Observers
{
    public sealed class VerifyMessageRequestObserverTests
    {
        private readonly ILogger _logger;
        private readonly IKeySigner _keySigner;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly byte[] _signatureBytes = ByteUtil.GenerateRandomByteArray(FFI.SignatureLength);
        private readonly byte[] _publicKeyBytes = ByteUtil.GenerateRandomByteArray(FFI.PublicKeyLength);
        private readonly SigningContext _signingContext = new SigningContext();
        private readonly string _message = "Any old message";

        public VerifyMessageRequestObserverTests()
        {            
            _keySigner = Substitute.For<IKeySigner>();
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyMessageRequest_Can_Send_VerifyMessageResponse(bool expectedResponse)
        {
            _keySigner.Verify(default, default, default).ReturnsForAnyArgs(expectedResponse);

            var messageFactory = new DtoFactory();

            var request = messageFactory.GetDto(
                new VerifyMessageRequest
                {
                    Message = _message.ToUtf8ByteString(),
                    PublicKey = RLP.EncodeElement(_publicKeyBytes).ToByteString(),
                    Signature = RLP.EncodeElement(_signatureBytes).ToByteString(),
                    SigningContext = _signingContext
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

            verifyResponseMessage.IsSignedByKey.Should().Be(expectedResponse);
        }
    }
}
