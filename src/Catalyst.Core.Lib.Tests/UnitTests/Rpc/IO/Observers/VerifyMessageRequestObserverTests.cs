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

using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using System.Linq;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    public sealed class VerifyMessageRequestObserverTests
    {
        private readonly IKeySigner _keySigner;
        private readonly VerifyMessageRequestObserver _verifyMessageRequestObserver;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IPeerIdentifier _testPeerIdentifier;
        private readonly VerifyMessageRequest _verifyMessageRequest;
        private readonly SigningContext _signingContext;

        public VerifyMessageRequestObserverTests()
        {
            _signingContext = new SigningContext
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.ProtocolRpc
            };

            _testPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("TestPeerIdentifier");

            _keySigner = Substitute.For<IKeySigner>();
            _keySigner.CryptoContext.Returns(new CryptoContext(new CryptoWrapper()));

            var logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();

            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
            _verifyMessageRequestObserver = new VerifyMessageRequestObserver(_testPeerIdentifier, logger, _keySigner);

            _verifyMessageRequest = GetValidVerifyMessageRequest();
        }

        [Fact]
        public void VerifyMessageRequestObserver_Can_Reject_Invalid_Public_Key_Length()
        {
            _verifyMessageRequest.PublicKey = ByteString.CopyFrom(new byte[FFI.PublicKeyLength + 1]);

            AssertVerifyResponse(false);
        }

        [Fact]
        public void VerifyMessageRequestObserver_Can_Reject_Invalid_Signature_Length()
        {
            _verifyMessageRequest.Signature = ByteString.CopyFrom(new byte[FFI.SignatureLength + 1]);
            AssertVerifyResponse(false);
        }

        [Fact]
        public void VerifyMessageRequestObserver_Can_Send_True_If_Valid_Signature()
        {
            _keySigner.Verify(default, default, default).ReturnsForAnyArgs(true);
            AssertVerifyResponse(true);
        }

        [Fact]
        public void VerifyMessageRequestObserver_Can_Send_False_Response_If_Verify_Fails()
        {
            _keySigner.Verify(default, default, default).ReturnsForAnyArgs(false);
            AssertVerifyResponse(false);
        }

        private VerifyMessageRequest GetValidVerifyMessageRequest()
        {
            var privateKey = _keySigner.CryptoContext.GeneratePrivateKey();
            var publicKey = privateKey.GetPublicKey();
            var messageToSign = ByteString.CopyFromUtf8("A Message to Sign");

            var verifyMessageRequest = new VerifyMessageRequest
            {
                Message = messageToSign,
                PublicKey = publicKey.Bytes.ToByteString(),
                Signature = _keySigner.CryptoContext.Sign(privateKey, messageToSign.ToByteArray(), _signingContext.ToByteArray()).SignatureBytes.ToByteString(),
                SigningContext = _signingContext
            };

            return verifyMessageRequest;
        }

        private void AssertVerifyResponse(bool valid)
        {
            _verifyMessageRequestObserver.OnNext(new ObserverDto(_fakeContext, _verifyMessageRequest.ToProtocolMessage(_testPeerIdentifier.PeerId)));

            var responseList = _fakeContext.Channel.ReceivedCalls().ToList();
            var response = ((MessageDto) responseList[0].GetArguments()[0]).Content
               .FromProtocolMessage<VerifyMessageResponse>();
            response.IsSignedByKey.Should().Be(valid);
        }
    }
}
