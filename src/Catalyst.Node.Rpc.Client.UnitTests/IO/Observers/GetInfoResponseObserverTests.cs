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
    public sealed class GetInfoResponseObserverTests : IDisposable
    {
        private readonly ILogger _logger;
        private GetInfoResponseObserver _observer;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IUserOutput _output;

        public GetInfoResponseObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        [Theory]
        [InlineData("Test")]
        [InlineData("Q2")]
        [InlineData("A Fake Info Response")]
        public async Task RpcClient_Can_Handle_GetInfoResponse(string query)
        {
            var response = new DtoFactory().GetDto(
                new GetInfoResponse
                {
                    Query = query
                },
                PeerIdentifierHelper.GetPeerIdentifier("sender"),
                PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                CorrelationId.GenerateCorrelationId()
            );

            var messageStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext,
                response.Content.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender_key").PeerId,
                    response.CorrelationId
                )
            );

            GetInfoResponse messageStreamResponse = null;

            _observer = new GetInfoResponseObserver(_output, _logger);
            _observer.StartObserving(messageStream);
            _observer.MessageResponseStream.Where(x => x.Message.GetType() == typeof(GetInfoResponse)).SubscribeOn(NewThreadScheduler.Default).Subscribe((RpcClientMessageDto) =>
            {
                messageStreamResponse = (GetInfoResponse) RpcClientMessageDto.Message;
            });

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            messageStreamResponse.Should().NotBeNull();
            messageStreamResponse.Query.Should().Be(response.Content.Query);
        }

        public void Dispose()
        {
            _observer?.Dispose();
        }
    }
}
