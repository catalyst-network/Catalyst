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
using Catalyst.Abstractions.Cli;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Modules.Rpc.Client.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Reactive.Testing;
using MultiFormats;
using NSubstitute;
using NUnit.Framework;
using Serilog;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    public sealed class SignMessageResponseHandlerTests : IDisposable
    {
        private ILogger _logger;
        private IChannelHandlerContext _fakeContext;
        public static readonly List<object[]> QueryContents = InitialiseQueryData();

        private IUserOutput _output;

        private SignMessageResponseObserver _handler;

        public struct SignedResponse : IEquatable<SignedResponse>
        {
            internal ByteString Signature;
            internal ByteString PublicKey;
            internal ByteString OriginalMessage;

            public bool Equals(SignedResponse other)
            {
                return (Signature == other.Signature)
                 && (OriginalMessage == other.OriginalMessage)
                 && (PublicKey == other.PublicKey);
            }
        }

        private static List<object[]> InitialiseQueryData()
        {
            return new List<object[]> 
            {
                new object[]
                {
                    SignMessage($@"hello", $@"this is a fake signature", $@"this is a fake public key")
                },
                new object[]
                {
                    SignMessage("", "", "")
                },
            };
        }

        [SetUp]
        public void Init()
        {
            _logger = Substitute.For<ILogger>();
            _output = Substitute.For<IUserOutput>();
        }

        private static SignedResponse SignMessage(string messageToSign, string signature, string pubKey)
        {
            var signedResponse = new SignedResponse
            {
                Signature = signature.ToUtf8ByteString(),
                PublicKey = pubKey.ToUtf8ByteString(),
                OriginalMessage = MultiBase.Encode(messageToSign.FromBase32()).ToUtf8ByteString()
            };

            return signedResponse;
        }

        [TestCaseSource(nameof(QueryContents))]
        public void RpcClient_Can_Handle_SignMessageResponse(SignedResponse signedResponse)
        {
            var testScheduler = new TestScheduler();

            var signMessageResponse = new SignMessageResponse
            {
                OriginalMessage = signedResponse.OriginalMessage,
                PublicKey = signedResponse.PublicKey,
                Signature = signedResponse.Signature,
            };

            var correlationId = CorrelationId.GenerateCorrelationId();

            var protocolMessage =
                signMessageResponse.ToProtocolMessage(MultiAddressHelper.GetAddress("sender"), correlationId);

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, testScheduler,
                protocolMessage
            );

            _handler = new SignMessageResponseObserver(_output, _logger);
            _handler.StartObserving(messageStream);

            testScheduler.Start();

            _output.Received(1).WriteLine(Arg.Any<string>());
        }

        public void Dispose()
        {
            _handler?.Dispose();
        }
    }
}
