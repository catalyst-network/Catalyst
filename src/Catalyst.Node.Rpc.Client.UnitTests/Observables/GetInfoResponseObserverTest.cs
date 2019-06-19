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

using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Rpc.Client.Observables;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using System;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.Observables
{
    public sealed class GetInfoResponseObserverTest : IDisposable
    {
        private readonly ILogger _logger;
        private GetInfoResponseObserver _requestObserver;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IUserOutput _output;

        public GetInfoResponseObserverTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        [Theory]
        [InlineData("Test")]
        [InlineData("Q2")]
        public void RpcClient_Can_Handle_GetInfoResponse(string query)
        {
            var getInfoResponse = new GetInfoResponse
            {
                Query = query
            };
            var response = new ProtocolMessageFactory().GetMessage(new MessageDto(
                    getInfoResponse,
                    MessageTypes.Response,
                    PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                    PeerIdentifierHelper.GetPeerIdentifier("sender")
                ),
                Guid.NewGuid());

            _requestObserver = new GetInfoResponseObserver(_output, _logger);
            _requestObserver.HandleResponse(new ProtocolMessageDto(_fakeContext, response));
            _output.Received(1).WriteLine(query);
        }

        public void Dispose()
        {
            _requestObserver?.Dispose();
        }
    }
}
