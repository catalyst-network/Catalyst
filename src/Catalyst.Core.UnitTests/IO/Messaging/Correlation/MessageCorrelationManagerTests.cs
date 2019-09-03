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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Handlers;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Protocol;
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
using PendingRequest = Catalyst.Core.IO.Messaging.Correlation.CorrelatableMessage<Catalyst.Protocol.Common.ProtocolMessage>;

namespace Catalyst.Core.UnitTests.IO.Messaging.Correlation
{
    public abstract class MessageCorrelationManagerTests<T>
        where T : IMessageCorrelationManager
    {
        protected readonly IPeerIdentifier[] PeerIds;
        protected List<PendingRequest> PendingRequests;

        protected T CorrelationManager;
        protected readonly ILogger SubbedLogger;
        protected readonly IChangeTokenProvider ChangeTokenProvider;
        protected readonly IMemoryCache Cache;
        private readonly PeerId _senderPeerId;

        protected readonly Dictionary<ByteString, ICacheEntry> CacheEntriesByRequest
            = new Dictionary<ByteString, ICacheEntry>();

        protected MessageCorrelationManagerTests()
        {
            SubbedLogger = Substitute.For<ILogger>();
            ChangeTokenProvider = Substitute.For<IChangeTokenProvider>();
            var changeToken = Substitute.For<IChangeToken>();
            ChangeTokenProvider.GetChangeToken().Returns(changeToken);
            Cache = Substitute.For<IMemoryCache>();

            _senderPeerId = PeerIdHelper.GetPeerId("sender");
            PeerIds = new[]
            {
                PeerIdentifierHelper.GetPeerIdentifier("peer1"),
                PeerIdentifierHelper.GetPeerIdentifier("peer2"),
                PeerIdentifierHelper.GetPeerIdentifier("peer3")
            };
        }

#pragma warning disable 693
        protected void PrepareCacheWithPendingRequests<T>()
#pragma warning restore 693
            where T : IMessage, new()
        {
            PendingRequests = PeerIds.Select((p, i) => new PendingRequest
            {
                Content = new T().ToProtocolMessage(_senderPeerId, CorrelationId.GenerateCorrelationId()),
                Recipient = p,
                SentAt = DateTimeOffset.MinValue.Add(TimeSpan.FromMilliseconds(100 * i))
            }).ToList();

            PendingRequests.ForEach(AddRequestExpectation);
        }

        private void AddRequestExpectation(PendingRequest pendingRequest)
        {
            Cache.TryGetValue(pendingRequest.Content.CorrelationId, out Arg.Any<object>())
               .Returns(ci =>
                {
                    ci[1] = pendingRequest;
                    return true;
                });
        }

        private void AddCreateEntryExpectation(object key)
        {
            var correlationId = (ByteString) key;
            var cacheEntry = Substitute.For<ICacheEntry>();
            var expirationTokens = new List<IChangeToken>();
            cacheEntry.ExpirationTokens.Returns(expirationTokens);
            var expirationCallbacks = new List<PostEvictionCallbackRegistration>();
            cacheEntry.PostEvictionCallbacks.Returns(expirationCallbacks);

            Cache.CreateEntry(correlationId).Returns(cacheEntry);
            CacheEntriesByRequest.Add(correlationId, cacheEntry);
        }

        [Fact]
        public virtual async Task New_Entries_Should_Be_Added_With_Individual_Entry_Options()
        {
            PendingRequests.ForEach(p => AddCreateEntryExpectation(p.Content.CorrelationId));
            PendingRequests.ForEach(CorrelationManager.AddPendingRequest);

            Cache.Received(PendingRequests.Count).CreateEntry(
                Arg.Is<object>(o => ((ByteString) o).ToCorrelationId().Id != Guid.Empty));

            var createEntryCalls = Cache.ReceivedCalls()
               .Where(ci => ci.GetMethodInfo().Name == nameof(IMemoryCache.CreateEntry));

            createEntryCalls.Select(c => c.GetArguments()[0]).Cast<ByteString>()
               .Should().BeEquivalentTo(PendingRequests.Select(p => p.Content.CorrelationId));

            foreach (var cacheEntry in CacheEntriesByRequest.Values)
            {
                cacheEntry.ExpirationTokens.Count.Should().Be(1);
                cacheEntry.PostEvictionCallbacks.Count.Should().Be(1);
            }

            CheckExpirationTokensAreDifferentForEachEntry();

            await CheckCacheEntriesCallback().ConfigureAwait(false);
        }

        private void CheckExpirationTokensAreDifferentForEachEntry()
        {
            for (var i = 0; i < CacheEntriesByRequest.Count; i++)
            for (var j = i + 1; j < CacheEntriesByRequest.Count; j++)
            {
                CacheEntriesByRequest[PendingRequests[i].Content.CorrelationId]
                   .Should().NotBeSameAs(CacheEntriesByRequest[PendingRequests[j].Content.CorrelationId]);
            }
        }

        protected abstract Task CheckCacheEntriesCallback();

        protected void FireEvictionCallBackByCorrelationId(ByteString correlationId)
        {
            var pendingRequest = PendingRequests.Single(p => p.Content.CorrelationId == correlationId);
            CacheEntriesByRequest[correlationId].PostEvictionCallbacks[0].EvictionCallback
               .Invoke(null, pendingRequest, EvictionReason.Expired, null);
        }

#pragma warning disable 693
        protected void TryMatchResponseAsync_Should_Match_Existing_Records_With_Matching_Correlation_Id<T>()
#pragma warning restore 693
            where T : IMessage, new()
        {
            var responseMatchingIndex1 = new T().ToProtocolMessage(
                PeerIds[1].PeerId,
                PendingRequests[1].Content.CorrelationId.ToCorrelationId());

            var request = CorrelationManager.TryMatchResponse(responseMatchingIndex1);
            request.Should().BeTrue();
        }

#pragma warning disable 693
        protected void TryMatchResponseAsync_Should_Not_Match_Existing_Records_With_Non_Matching_Correlation_Id<T>()
#pragma warning restore 693
            where T : IMessage, new()
        {
            var responseMatchingNothing =
                new T().ToProtocolMessage(PeerIds[1].PeerId, CorrelationId.GenerateCorrelationId());

            var request = CorrelationManager.TryMatchResponse(responseMatchingNothing);
            request.Should().BeFalse();
        }

        [Fact]
        public void UncorrelatedMessage_Should_Not_Propagate_To_Next_Pipeline()
        {
            var correlationManager = Substitute.For<IMessageCorrelationManager>();
            correlationManager.TryMatchResponse(Arg.Any<ProtocolMessage>()).Returns(false);

            var correlationHandler = new CorrelationHandler<IMessageCorrelationManager>(correlationManager);
            var channelHandlerContext = Substitute.For<IChannelHandlerContext>();
            var nonCorrelatedMessage =
                new PingResponse().ToProtocolMessage(PeerIds[0].PeerId, CorrelationId.GenerateCorrelationId());
            correlationHandler.ChannelRead(channelHandlerContext, nonCorrelatedMessage);

            channelHandlerContext.DidNotReceive().FireChannelRead(nonCorrelatedMessage);
        }

        [Fact]
        public void TryMatchResponseAsync_Should_Not_Match_On_Wrong_Response_Type()
        {
            var matchingRequest = PendingRequests[2].Content;
            var matchingRequestWithWrongType =
                new PeerNeighborsResponse().ToProtocolMessage(matchingRequest.PeerId,
                    matchingRequest.CorrelationId.ToCorrelationId());

            new Action(() => CorrelationManager.TryMatchResponse(matchingRequestWithWrongType))
               .Should().Throw<InvalidDataException>()
               .And.Message.Should().Contain(PeerNeighborsResponse.Descriptor.ShortenedFullName());
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
