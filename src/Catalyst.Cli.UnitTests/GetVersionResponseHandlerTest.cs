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
using System.Threading.Tasks;
using Catalyst.Cli.Handlers;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Cli.UnitTests
{
    public sealed class GetVersionResponseHandlerTest : IDisposable
    {
        private readonly IUserOutput _output;
        public static readonly List<object[]> QueryContents;
        private readonly IChannelHandlerContext _fakeContext;

        private readonly ILogger _logger;
        private GetVersionResponseHandler _handler;

        static GetVersionResponseHandlerTest()
        {
            QueryContents = new List<object[]>
            {
                new object[]
                {
                    "0.0.0.0"
                },
                new object[] {""}
            };
        }

        public GetVersionResponseHandlerTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        private IObservable<ProtocolMessageDto> CreateStreamWithMessage(ProtocolMessage response)
        {
            var channeledAny = new ProtocolMessageDto(_fakeContext, response);
            var messageStream = new[] {channeledAny}.ToObservable();
            return messageStream;
        }

        [Theory]
        [MemberData(nameof(QueryContents))]
        public async Task RpcClient_Can_Handle_GetVersionResponse(string version)
        {
            var response = new MessageFactory().GetMessage(new MessageDto(
                    new VersionResponse
                    {
                        Version = version
                    },
                    MessageTypes.Tell,
                    PeerIdentifierHelper.GetPeerIdentifier("recpient"),
                    PeerIdentifierHelper.GetPeerIdentifier("sender")
                ),
                Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, response);

            _handler = new GetVersionResponseHandler(_output, _logger);
            _handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolScheduler();

            _output.Received(1).WriteLine($"Node Version: {version}");
        }

        public void Dispose()
        {
            _handler?.Dispose();
        }
    }
}
