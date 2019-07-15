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
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NSubstitute.Core;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using PendingRequest = Catalyst.Common.IO.Messaging.Correlation.CorrelatableMessage<Catalyst.Protocol.Common.ProtocolMessage>;

namespace Catalyst.Common.UnitTests.IO.Messaging.Correlation
{
    public abstract class MessageCorrelationManagerTests<T> 
        where T : IMessageCorrelationManager
    {
        protected readonly IPeerIdentifier[] PeerIds;
        protected List<PendingRequest> PendingRequests;

        protected T CorrelationManager;
        protected readonly ILogger SubbedLogger;
        protected readonly IChangeTokenProvider ChangeTokenProvider;
        protected readonly IChangeToken ChangeToken;
        protected readonly IMemoryCache Cache;
        protected readonly PeerId SenderPeerId;

        protected readonly Dictionary<ByteString, ICacheEntry> CacheEntriesByRequest 
            = new Dictionary<ByteString, ICacheEntry>();
        
        protected MessageCorrelationManagerTests(ITestOutputHelper output)
        {
            SubbedLogger = Substitute.For<ILogger>();
            ChangeTokenProvider = Substitute.For<IChangeTokenProvider>();
            ChangeToken = Substitute.For<IChangeToken>();
            ChangeTokenProvider.GetChangeToken().Returns(ChangeToken);
            Cache = Substitute.For<IMemoryCache>();

            SenderPeerId = PeerIdHelper.GetPeerId("sender");
            PeerIds = new[]
            {
                PeerIdentifierHelper.GetPeerIdentifier("peer1"),
                PeerIdentifierHelper.GetPeerIdentifier("peer2"),
                PeerIdentifierHelper.GetPeerIdentifier("peer3"),
            };
        }

        protected void PrepareCacheWithPendingRequests<T>()
            where T : IMessage, new()
        {
            PendingRequests = PeerIds.Select((p, i) => new CorrelatableMessage<ProtocolMessage>
            {
                Content = new T().ToProtocolMessage(SenderPeerId, CorrelationId.GenerateCorrelationId()),
                Recipient = p,
                SentAt = DateTimeOffset.MinValue.Add(TimeSpan.FromMilliseconds(100 * i))
            }).ToList();

            PendingRequests.ForEach(AddRequestExpectation);
        }

        private void AddRequestExpectation(CorrelatableMessage<ProtocolMessage> pendingRequest)
        {
            Cache.TryGetValue(pendingRequest.Content.CorrelationId, out Arg.Any<object>())
               .Returns(ci =>
                {
                    ci[1] = pendingRequest;
                    return true;
                });
        }

        private void AddCreateCacheEntryExpectation(object key)
        {
            var correlationId = (ByteString) key;
            var cacheEntry = Substitute.For<ICacheEntry>();
            var expirationTokens = new List<IChangeToken>();
            cacheEntry.ExpirationTokens.Returns(expirationTokens);
            var expirationCallbacks = new List<PostEvictionCallbackRegistration>();
            cacheEntry.PostEvictionCallbacks.Returns(expirationCallbacks);

            Cache.CreateEntry(correlationId).Returns(cacheEntry);
        }

        [Fact]
        public virtual async Task New_Entries_Should_Be_Added_With_Individual_Entry_Options()
        {
            PendingRequests.ForEach(p => AddCreateCacheEntryExpectation(p.Content.CorrelationId.ToCorrelationId()));
            PendingRequests.ForEach(CorrelationManager.AddPendingRequest);

            Cache.Received(PendingRequests.Count).CreateEntry(
                Arg.Is<object>(o => ((ByteString) o).ToCorrelationId().Id != Guid.Empty));

            var createEntryCalls = Cache.ReceivedCalls()
               .Where(ci => ci.GetMethodInfo().Name == nameof(IMemoryCache.CreateEntry));

            createEntryCalls.Select(c => c.GetArguments()[0]).Cast<ICorrelationId>()
               .Should().BeEquivalentTo(PendingRequests.Select(p => p.Content.CorrelationId));

            foreach (var cacheEntry in CacheEntriesByRequest.Values)
            {
                cacheEntry.ExpirationTokens.Count.Should().Be(1);
                cacheEntry.PostEvictionCallbacks.Count.Should().Be(1);
            }

            CheckExpirationTokensAreDifferentForEachEntry();

            CheckCacheEntriesCallback();
        }

        private void CheckExpirationTokensAreDifferentForEachEntry()
        {
            var x = new List<int>();

            for (var i = 0; i < CacheEntriesByRequest.Count; i++)
            for (var j = i + 1; j < CacheEntriesByRequest.Count; j++)
            {
                CacheEntriesByRequest[PendingRequests[i].Content.CorrelationId]
                   .Should().NotBeSameAs(CacheEntriesByRequest[PendingRequests[j].Content.CorrelationId]);
            }
        }

        protected abstract void CheckCacheEntriesCallback();

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
        }

        [Fact]
        public void TryMatchResponseAsync_should_not_match_existing_records_with_non_matching_correlation_id()
        {
            var responseMatchingNothing = new PingResponse().ToProtocolMessage(PeerIds[1].PeerId, CorrelationId.GenerateCorrelationId());
         
            var request = CorrelationManager.TryMatchResponse(responseMatchingNothing);
            request.Should().BeFalse();
        }

        [Fact(Skip = "Think underlying functionality changed considerably since this was written need to confirm")] // @TODO 
        public void TryMatchResponseAsync_should_not_match_on_wrong_response_type()
        {
            var matchingRequest = PendingRequests[3].Content;
         
            new Action(() => CorrelationManager.TryMatchResponse(matchingRequest))
               .Should().Throw<ArgumentException>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            
            Cache?.Dispose();
            CorrelationManager?.Dispose();
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
