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
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Common.UnitTests.IO.Messaging
{
    public abstract class MessageCorrelationManagerTests<T> 
        where T : IMessageCorrelationManager, IDisposable
    {
        protected readonly IPeerIdentifier[] PeerIds;
        protected readonly IList<CorrelatableMessage> PendingRequests;

        protected T CorrelationManager;
        protected readonly ILogger SubbedLogger;
        protected readonly IChangeTokenProvider ChangeTokenProvider;
        protected readonly IChangeToken ChangeToken;
        protected MemoryCache Cache;

        protected MessageCorrelationManagerTests(ITestOutputHelper output)
        {
            SubbedLogger = Substitute.For<ILogger>();
            ChangeTokenProvider = Substitute.For<IChangeTokenProvider>();
            ChangeToken = Substitute.For<IChangeToken>();
            ChangeTokenProvider.GetChangeToken().Returns(ChangeToken);
            Cache = new MemoryCache(new MemoryCacheOptions());

            var senderPeerId = PeerIdHelper.GetPeerId("sender");
            PeerIds = new[]
            {
                PeerIdHelper.GetPeerId("abcd"),
                PeerIdHelper.GetPeerId("efgh"),
                PeerIdHelper.GetPeerId("ijkl")
            }.Select(p => new PeerIdentifier(p) as IPeerIdentifier).ToArray();

            PendingRequests = PeerIds.Select((p, i) => new CorrelatableMessage
            {
                Content = new PingRequest().ToProtocolMessage(senderPeerId, CorrelationId.GenerateCorrelationId()),
                Recipient = p,
                SentAt = DateTimeOffset.MinValue.Add(TimeSpan.FromMilliseconds(100 * i))
            }).ToList();

            var responseStore = Substitute.For<IMemoryCache>();
            responseStore.TryGetValue(Arg.Any<ByteString>(), out Arg.Any<CorrelatableMessage>())
               .Returns(ci =>
                {
                    output.WriteLine("");
                    ci[1] = PendingRequests.SingleOrDefault(
                        r => r.Content.CorrelationId.ToBase64() == ((ByteString) ci[0]).ToBase64());
                    return ci[1] != null;
                });
        }

        public abstract Task RequestStore_Should_Not_Keep_Records_For_Longer_Than_Ttl();

        [Fact]
        public void TryMatchResponseAsync_should_match_existing_records_with_matching_correlation_id()
        {
            var responseMatchingIndex1 = new PingResponse().ToProtocolMessage(
                PeerIds[1].PeerId,
                PendingRequests[1].Content.CorrelationId.ToCorrelationId());

            var request = CorrelationManager.TryMatchResponse(responseMatchingIndex1);
            request.Should().BeTrue();
        }

        [Fact]
        public void UncorrelatedMessage_should_not_propagate_to_next_pipeline()
        {
            var correlationManager = Substitute.For<IMessageCorrelationManager>();
            correlationManager.TryMatchResponse(Arg.Any<ProtocolMessage>()).Returns(false);

            var correlationHandler = new CorrelationHandler<IMessageCorrelationManager>(correlationManager);
            var channelHandlerContext = Substitute.For<IChannelHandlerContext>();
            var nonCorrelatedMessage = new PingResponse().ToProtocolMessage(PeerIds[0].PeerId, CorrelationId.GenerateCorrelationId());
            correlationHandler.ChannelRead(channelHandlerContext, nonCorrelatedMessage);

            channelHandlerContext.DidNotReceive().FireChannelRead(nonCorrelatedMessage);
            
            channelHandlerContext.Received().CloseAsync();
        }

        [Fact]
        public void TryMatchResponseAsync_should_not_match_existing_records_with_non_matching_correlation_id()
        {
            var responseMatchingNothing = new PingResponse().ToProtocolMessage(PeerIds[1].PeerId, CorrelationId.GenerateCorrelationId());
         
            var request = CorrelationManager.TryMatchResponse(responseMatchingNothing);
            request.Should().BeTrue();
        }

        [Fact]
        public void TryMatchResponseAsync_should_not_match_on_wrong_response_type()
        {
            var matchingRequest = PendingRequests[1].Content;
         
            new Action(() => CorrelationManager.TryMatchResponse(matchingRequest))
               .Should().Throw<ArgumentException>();
        }
        
        public void Dispose()
        {
            Cache?.Dispose();
            CorrelationManager?.Dispose();
        }
    }
}
