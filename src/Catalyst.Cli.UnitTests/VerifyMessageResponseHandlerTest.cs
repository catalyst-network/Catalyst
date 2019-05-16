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
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Rpc;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Cli.UnitTests
{
    public sealed class VerifyMessageResponseHandlerTest : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IUserOutput _output;

        public static List<object[]> QueryContents;

        private VerifyMessageResponseHandler _handler;
        private readonly IRpcCorrelationCache _subbedCorrelationCache;

        static VerifyMessageResponseHandlerTest()
        {
            QueryContents = new List<object[]>
            {
                new object[]
                {
                    true
                },
                new object[]
                {
                    false
                }
            };
        }

        public VerifyMessageResponseHandlerTest()
        {
            _subbedCorrelationCache = Substitute.For<IRpcCorrelationCache>();
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        [Theory]
        [MemberData(nameof(QueryContents))]
        public void RpcClient_Can_Handle_VerifyMessageResponse(bool isSignedByNode)
        {
            var response = new RpcMessageFactory(_subbedCorrelationCache).GetMessage(new MessageDto(
                    new VerifyMessageResponse
                    {
                        IsSignedByKey = isSignedByNode
                    },
                    MessageTypes.Tell,
                    PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                    PeerIdentifierHelper.GetPeerIdentifier("sender")
                ),
                Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, response);
            var cache = Substitute.For<IMessageCorrelationCache>();

            _handler = new VerifyMessageResponseHandler(_output, cache, _logger);
            _handler.StartObservingMessageStreams(messageStream);

            _output.Received(1).WriteLine(isSignedByNode.ToString());
        }

        public void Dispose()
        {
            _handler.Dispose();
        }
    }
}
