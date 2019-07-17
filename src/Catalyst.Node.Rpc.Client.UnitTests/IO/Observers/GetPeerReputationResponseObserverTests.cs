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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Rpc.Client.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.IO.Observers
{
    /// <summary>
    /// Tests the CLI for peer reputation response
    /// </summary>
    public sealed class GetPeerReputationResponseObserverTests : IDisposable
    {
        private readonly IUserOutput _output;
        public static readonly List<object[]> QueryContents;
        private readonly IChannelHandlerContext _fakeContext;

        private readonly ILogger _logger;
        private PeerReputationResponseObserver _observer;

        /// <summary>
        /// Initializes the <see>
        ///     <cref>GetPeerReputationResponseObserverTest</cref>
        /// </see>
        /// class.
        /// </summary>
        static GetPeerReputationResponseObserverTests()
        {             
            QueryContents = new List<object[]>
            {
                new object[] {78},
                new object[] {1572},
                new object[] {22}
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see>
        ///     <cref>GetPeerReputationResponseObserverTest</cref>
        /// </see>
        /// class.
        /// </summary>
        public GetPeerReputationResponseObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        /// <summary>
        /// RPCs the client can handle get reputation response.
        /// </summary>
        /// <param name="rep">The rep.</param>
        [Theory]
        [MemberData(nameof(QueryContents))]
        [InlineData(int.MinValue)]
        public async Task RpcClient_Can_Handle_GetReputationResponse(int rep)
        {
            var getPeerReputationResponse = await TestGetReputationResponse(rep).ConfigureAwait(false);
            getPeerReputationResponse.Should().NotBeNull();
            getPeerReputationResponse.Reputation.Should().Be(rep);
        }

        private async Task<GetPeerReputationResponse> TestGetReputationResponse(int rep)
        {
            var response = new DtoFactory().GetDto(new GetPeerReputationResponse
                {
                    Reputation = rep
                },
                PeerIdentifierHelper.GetPeerIdentifier("sender"),
                PeerIdentifierHelper.GetPeerIdentifier("recpient"),
                CorrelationId.GenerateCorrelationId()
            );

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext,
                response.Content.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId,
                    response.CorrelationId));

            GetPeerReputationResponse messageStreamResponse = null;

            _observer = new PeerReputationResponseObserver(_output, _logger);
            _observer.StartObserving(messageStream);
            _observer.MessageResponseStream.Where(x => x.Message.GetType() == typeof(GetPeerReputationResponse)).SubscribeOn(NewThreadScheduler.Default).Subscribe((RpcClientMessageDto) =>
            {
                messageStreamResponse = (GetPeerReputationResponse)RpcClientMessageDto.Message;
            });

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync().ConfigureAwait(false);

            return messageStreamResponse;
        }

        public void Dispose()
        {
            _observer?.Dispose();
        }
    }
}
