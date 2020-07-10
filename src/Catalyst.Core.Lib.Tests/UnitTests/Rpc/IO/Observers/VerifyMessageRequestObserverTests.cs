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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using System.Linq;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.TestUtils.Fakes;
using NUnit.Framework;
using MultiFormats;
using Catalyst.Abstractions.P2P;
using Catalyst.Protocol.Wire;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    public sealed class VerifyMessageRequestObserverTests
    {
        private IKeySigner _keySigner;
        private VerifyMessageRequestObserver _verifyMessageRequestObserver;
        private IChannelHandlerContext _fakeContext;
        private MultiAddress _testPeerId;
        private VerifyMessageRequest _verifyMessageRequest;
        private SigningContext _signingContext;
        private IPeerClient _peerClient;

        [SetUp]
        public void Init()
        {
            _signingContext = new SigningContext
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.ProtocolRpc
            };

            _peerClient = Substitute.For<IPeerClient>();

            _testPeerId = MultiAddressHelper.GetAddress("TestPeerIdentifier");

            var peerSettings = _testPeerId.ToSubstitutedPeerSettings();

            _keySigner = Substitute.For<FakeKeySigner>();
            _keySigner.CryptoContext.Returns(new FfiWrapper());

            var logger = Substitute.For<ILogger>();

            _verifyMessageRequestObserver = new VerifyMessageRequestObserver(peerSettings, _peerClient, logger, _keySigner);

            _verifyMessageRequest = GetValidVerifyMessageRequest();
        }

        [Test]
        public void VerifyMessageRequestObserver_Can_Reject_Invalid_Public_Key_Length()
        {
            _verifyMessageRequest.PublicKey = ByteString.CopyFrom(new byte[new FfiWrapper().PublicKeyLength + 1]);

            AssertVerifyResponse(false);
        }

        [Test]
        public void VerifyMessageRequestObserver_Can_Reject_Invalid_Signature_Length()
        {
            _verifyMessageRequest.Signature = ByteString.CopyFrom(new byte[new FfiWrapper().SignatureLength + 1]);
            AssertVerifyResponse(false);
        }

        [Test]
        public void VerifyMessageRequestObserver_Can_Send_True_If_Valid_Signature()
        {
            _keySigner.Verify(default, default, default).ReturnsForAnyArgs(true);
            AssertVerifyResponse(true);
        }

        [Test]
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
            _verifyMessageRequestObserver.OnNext(_verifyMessageRequest.ToProtocolMessage(_testPeerId));

            var responseList = _peerClient.ReceivedCalls().ToList();
            var response = ((ProtocolMessage) responseList[0].GetArguments()[0]).FromProtocolMessage<VerifyMessageResponse>();
            response.IsSignedByKey.Should().Be(valid);
        }
    }
}
