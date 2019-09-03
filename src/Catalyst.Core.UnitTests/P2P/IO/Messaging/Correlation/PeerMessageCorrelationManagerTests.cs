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
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P.ReputationSystem;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.P2P.IO.Messaging.Correlation;
using Catalyst.Core.P2P.ReputationSystem;
using Catalyst.Core.UnitTests.IO.Messaging.Correlation;
using Catalyst.Protocol.IPPN;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P.IO.Messaging.Correlation
{
    public sealed class PeerMessageCorrelationManagerTests : MessageCorrelationManagerTests<IPeerMessageCorrelationManager>
    {
        private readonly TestScheduler _testScheduler;
        private readonly Dictionary<IPeerIdentifier, int> _reputationByPeerIdentifier;

        public PeerMessageCorrelationManagerTests()
        {
            _testScheduler = new TestScheduler();

            var subbedRepManager = Substitute.For<IReputationManager>();
            
            CorrelationManager = new PeerMessageCorrelationManager(subbedRepManager,
                Cache,
                SubbedLogger,
                ChangeTokenProvider,
                _testScheduler
            );

            _reputationByPeerIdentifier = PeerIds.ToDictionary(p => p, p => 0);

            PrepareCacheWithPendingRequests<PingRequest>();

            CorrelationManager.ReputationEventStream.Subscribe(change =>
            {
                if (!_reputationByPeerIdentifier.ContainsKey(change.PeerIdentifier))
                {
                    return;
                }
                
                _reputationByPeerIdentifier[change.PeerIdentifier] += change.ReputationEvent.Amount;
            });
        }

        [Fact]
        public void TryMatchResponseAsync_Should_Match_Existing_Records_With_Matching_Correlation_Id()
        {
            TryMatchResponseAsync_Should_Match_Existing_Records_With_Matching_Correlation_Id<PingResponse>();
        }

        [Fact]
        public void TryMatchResponseAsync_Should_Not_Match_Existing_Records_With_Non_Matching_Correlation_Id()
        {
            TryMatchResponseAsync_Should_Not_Match_Existing_Records_With_Non_Matching_Correlation_Id<PingResponse>();
        }

        [Fact]
        public void TryMatchResponseAsync_when_matching_should_increase_reputation()
        {
            var reputationBefore = _reputationByPeerIdentifier[PeerIds[1]];

            var responseMatchingIndex1 = new PingResponse().ToProtocolMessage(
                PeerIds[1].PeerId,
                PendingRequests[1].Content.CorrelationId.ToCorrelationId());

            var request = CorrelationManager.TryMatchResponse(responseMatchingIndex1);
            request.Should().BeTrue();

            _testScheduler.Start();

            var reputationAfter = _reputationByPeerIdentifier[PeerIds[1]];
            reputationAfter.Should().BeGreaterThan(reputationBefore);

            _reputationByPeerIdentifier.Where(r => !r.Key.Equals(PeerIds[1]))
               .Select(r => r.Value).Should().AllBeEquivalentTo(0);
        }

        [Fact]
        public void UncorrelatedMessage_should_decrease_reputation()
        {
            var reputationBefore = _reputationByPeerIdentifier[PeerIds[1]];
            var responseMatchingIndex1 = new PingResponse().ToProtocolMessage(
                PeerIds[1].PeerId,
                CorrelationId.GenerateCorrelationId());

            CorrelationManager.TryMatchResponse(responseMatchingIndex1);

            _testScheduler.Start();

            var reputationAfter = _reputationByPeerIdentifier[PeerIds[1]];
            reputationAfter.Should().BeLessThan(reputationBefore);
        }

#pragma warning disable 1998
        protected override async Task CheckCacheEntriesCallback()
#pragma warning restore 1998
        {
            var observer = Substitute.For<IObserver<IPeerReputationChange>>();
            using (CorrelationManager.ReputationEventStream.Subscribe(observer))
            {
                var firstCorrelationId = PendingRequests[0].Content.CorrelationId;
                FireEvictionCallBackByCorrelationId(firstCorrelationId);

                _testScheduler.Start();

                observer.Received(1).OnNext(Arg.Is<IPeerReputationChange>(c => c.PeerIdentifier.PeerId.Equals(PendingRequests[0].Content.PeerId) 
                 && c.ReputationEvent.Equals(ReputationEventType.NoResponseReceived)));
            }
        }

        private ByteString FireEvictionCallBackByPendingRequestIndex()
        {
            var contentCorrelationId = PendingRequests[0].Content.CorrelationId;
            CacheEntriesByRequest[contentCorrelationId].PostEvictionCallbacks[0].EvictionCallback
               .Invoke(null, PendingRequests[0], EvictionReason.Expired, null);
            return contentCorrelationId;
        }
    }
}
