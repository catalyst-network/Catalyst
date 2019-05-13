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
using System.Reactive.Linq;
using Catalyst.Cli.Handlers;
using Catalyst.Common.Config;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Cli.UnitTests
{
    /// <summary>
    /// Tests the CLI for peer reputation response
    /// </summary>
    public sealed class GetPeerReputationResponseHandlerTest : IDisposable
    {
        private readonly IUserOutput _output;
        public static readonly List<object[]> QueryContents;
        private readonly IChannelHandlerContext _fakeContext;

        private readonly ILogger _logger;
        private PeerReputationResponseHandler _handler;

        /// <summary>
        /// Initializes the <see cref="GetPeerReputationResponseHandlerTest"/> class.
        /// </summary>
        static GetPeerReputationResponseHandlerTest()
        {
            QueryContents = new List<object[]>()
            {
                new object[] {78},
                new object[] {1572},
                new object[] {22}
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPeerReputationResponseHandlerTest"/> class.
        /// </summary>
        public GetPeerReputationResponseHandlerTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        /// <summary>
        /// Creates the stream with message.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns></returns>
        private IObservable<ChanneledAnySigned> CreateStreamWithMessage(AnySigned response)
        {
            var channeledAny = new ChanneledAnySigned(_fakeContext, response);
            var messageStream = new[] {channeledAny}.ToObservable();
            return messageStream;
        }
        
        /// <summary>
        /// RPCs the client can handle get reputation response.
        /// </summary>
        /// <param name="rep">The rep.</param>
        [Theory]
        [MemberData(nameof(QueryContents))]
        public void RpcClient_Can_Handle_GetReputationResponse(int rep)
        {
            TestGetReputationResponse(rep);

            _output.Received(1).WriteLine($"Peer Reputation: {rep}");
        }

        /// <summary>
        /// RPCs the client can handle get reputation response non existant peers.
        /// </summary>
        /// <param name="rep">The rep.</param>
        [Theory]
        [InlineData(int.MinValue)]
        public void RpcClient_Can_Handle_GetReputationResponseNonExistantPeers(int rep)
        {
            TestGetReputationResponse(rep);

            _output.Received(1).WriteLine($"Peer Reputation: Peer not found");
        }

        private void TestGetReputationResponse(int rep)
        {
            var correlationCache = Substitute.For<IMessageCorrelationCache>();

            var response = new RpcMessageFactory<GetPeerReputationResponse>().GetMessage(
                new GetPeerReputationResponse
                {
                    Reputation = rep
                },
                PeerIdentifierHelper.GetPeerIdentifier("recpient"),
                PeerIdentifierHelper.GetPeerIdentifier("sender"),
                MessageTypes.Ask,
                Guid.NewGuid());

            var messageStream = CreateStreamWithMessage(response);

            _handler = new PeerReputationResponseHandler(_output, correlationCache, _logger);
            _handler.StartObserving(messageStream);
        }

        public void Dispose()
        {
            _handler?.Dispose();
        }
    }
}
