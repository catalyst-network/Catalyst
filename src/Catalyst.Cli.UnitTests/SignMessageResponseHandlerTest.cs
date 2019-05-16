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
using Catalyst.Cli.Handlers;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Rpc;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Cli.UnitTests 
{
    public sealed class SignMessageResponseHandlerTest : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        public static readonly List<object[]> QueryContents;
        
        private readonly IUserOutput _output;
        private SignMessageResponseHandler _handler;
        private static IMessageCorrelationCache _subbedCorrelationCache;

        //@TODO why not mock the actual response object? if we ever change it then the test will pass but fail in real world
        public struct SignedResponse
        {
            internal ByteString Signature;
            internal ByteString PublicKey;
            internal ByteString OriginalMessage;
        }

        static SignMessageResponseHandlerTest()
        {   
            _subbedCorrelationCache = Substitute.For<IMessageCorrelationCache>();
            QueryContents = new List<object[]>
            {
                new object[]
                {
                    SignMessage("hello", "this is a fake signature", "this is a fake public key")
                },
                new object[]
                {
                    SignMessage("", "", "")
                },
            };
        }
        
        public SignMessageResponseHandlerTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        public static SignedResponse SignMessage(string messageToSign, string signature, string pubKey)
        {
            var signedResponse = new SignedResponse
            {
                Signature = signature.ToUtf8ByteString(),
                PublicKey = pubKey.ToUtf8ByteString(),
                OriginalMessage = RLP.EncodeElement(messageToSign.ToBytesForRLPEncoding()).ToByteString()
            };

            return signedResponse;
        }

        [Theory]
        [MemberData(nameof(QueryContents))]  
        public void RpcClient_Can_Handle_SignMessageResponse(SignedResponse signedResponse)
        {   
            var correlationCache = Substitute.For<IMessageCorrelationCache>();

            var response = new RpcMessageFactory<SignMessageResponse>(_subbedCorrelationCache).GetMessage(
                new SignMessageResponse
                {
                    OriginalMessage = signedResponse.OriginalMessage,
                    PublicKey = signedResponse.PublicKey,
                    Signature = signedResponse.Signature
                },
                PeerIdentifierHelper.GetPeerIdentifier("recipient_key"),
                PeerIdentifierHelper.GetPeerIdentifier("sender_key"),
                MessageTypes.Tell,
                Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, response);
            
            _handler = new SignMessageResponseHandler(_output, correlationCache, _logger);
            _handler.StartObserving(messageStream);
            
            _output.Received(1).WriteLine(Arg.Any<string>());
        }
        
        public void Dispose()
        {
            _handler?.Dispose();
        }
    }
}
