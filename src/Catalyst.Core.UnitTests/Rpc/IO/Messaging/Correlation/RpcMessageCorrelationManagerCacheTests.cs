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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Rpc.IO.Messaging.Correlation;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Rpc.IO.Messaging.Correlation
{
    public sealed class RpcMessageCorrelationManagerCacheTests : IDisposable
    {
        public RpcMessageCorrelationManagerCacheTests()
        {
            _testScheduler = new TestScheduler();

            var memoryCacheOptions = new MemoryCacheOptions();
            var memoryCache = new MemoryCache(memoryCacheOptions);

            var logger = Substitute.For<ILogger>();
            var changeTokenProvider = Substitute.For<IChangeTokenProvider>();

            _cancellationTokenSource = new CancellationTokenSource();
            var expirationToken = new CancellationChangeToken(_cancellationTokenSource.Token);
            changeTokenProvider.GetChangeToken().Returns(expirationToken);

            _rpcMessageCorrelationManager =
                new RpcMessageCorrelationManager(memoryCache, logger, changeTokenProvider, _testScheduler);
        }

        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly TestScheduler _testScheduler;

        private readonly RpcMessageCorrelationManager _rpcMessageCorrelationManager;

        [Fact]
        public void Dispose_Should_Dispose_RpcMessageCorrelationManager() { _rpcMessageCorrelationManager.Dispose(); }

        [Fact]
        public async Task Message_Eviction_Should_Cause_Eviction_Event()
        {
            var peerIds = new[]
            {
                PeerIdentifierHelper.GetPeerIdentifier("peer1"),
                PeerIdentifierHelper.GetPeerIdentifier("peer2"),
                PeerIdentifierHelper.GetPeerIdentifier("peer3")
            };

            var pendingRequests = peerIds.Select(peerId => new CorrelatableMessage<ProtocolMessage>
            {
                Content = new VersionRequest().ToProtocolMessage(PeerIdHelper.GetPeerId("sender"),
                    CorrelationId.GenerateCorrelationId()),
                Recipient = peerId,
                SentAt = DateTimeOffset.MinValue.Add(TimeSpan.Zero)
            }).ToList();

            var pendingResponses = pendingRequests.Select(peerId => new CorrelatableMessage<ProtocolMessage>
            {
                Content = new VersionResponse().ToProtocolMessage(PeerIdHelper.GetPeerId("sender"),
                    peerId.Content.CorrelationId.ToCorrelationId()),
                Recipient = peerId.Recipient,
                SentAt = DateTimeOffset.MinValue.Add(TimeSpan.Zero)
            }).ToList();

            pendingRequests.ForEach(_rpcMessageCorrelationManager.AddPendingRequest);

            //Log the number of evicted events
            var evictedEvents = 0;

            //Subscribe to eviction events, so we can count them and assert against them.
            _rpcMessageCorrelationManager.EvictionEvents.Subscribe(response => evictedEvents++);

            //Evict all messages.
            _cancellationTokenSource.Cancel();

            //Required to evict the cache, microsoft removed the timer in newer versions but read will if the cache has expired
            //https://referencesource.microsoft.com/#System.Runtime.Caching/System/Caching/MemoryCacheStore.cs,ae04d12e168ec1ab
            pendingResponses.ForEach(pendingResponse =>
                _rpcMessageCorrelationManager.TryMatchResponse(pendingResponse.Content));

            //To prevent cache eviction multi threading delay
            const int milliseconds = 100;
            while (evictedEvents < pendingRequests.Count)
            {
                _testScheduler.AdvanceBy(milliseconds);
                await Task.Delay(milliseconds).ConfigureAwait(false);
            }

            evictedEvents.Should().Be(pendingRequests.Count);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _rpcMessageCorrelationManager?.Dispose();
        }
    }
}
