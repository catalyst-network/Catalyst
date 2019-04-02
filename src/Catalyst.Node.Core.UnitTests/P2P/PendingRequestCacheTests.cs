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
using System.Linq.Expressions;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using FluentAssertions;
using NSubstitute;
using SharpRepository.Repository;
using Xunit;
using PendingRequest = Catalyst.Node.Common.P2P.PendingRequest;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public class PendingRequestCacheTests
    {
        private readonly IPeerIdentifier[] _peerIds;
        private readonly IList<PendingRequest> _pendingRequests;
        private readonly PendingRequestCache _cache;
        private PeerId _senderPeerId;
        private Dictionary<IPeerIdentifier, int> _reputationByPeerIdentifier;

        public PendingRequestCacheTests()
        {
            _senderPeerId = PeerIdentifierHelper.GetPeerId("sender");
            _peerIds = new []
            {
                PeerIdentifierHelper.GetPeerId("abcd"),
                PeerIdentifierHelper.GetPeerId("efgh"),
                PeerIdentifierHelper.GetPeerId("ijkl"),
            }.Select(p => new PeerIdentifier(p) as IPeerIdentifier).ToArray();

            _reputationByPeerIdentifier = _peerIds.ToDictionary(p => p, p => 0);

            _pendingRequests = _peerIds.Select((p, i) => new PendingRequest()
            {
                SentTo = p,
                Content = new PingRequest {
                        CorrelationId = Guid.NewGuid().ToByteArray().ToByteString(),
                    }.ToAnySigned(_senderPeerId),
                SentAt = DateTimeOffset.MinValue
                   .Add(TimeSpan.FromMilliseconds(100 * i))
            }).ToList();

            var responseStore = Substitute.For<IRepository<PendingRequest>>();
            responseStore.TryFind(Arg.Any<Expression<Func<PendingRequest, bool>>>(),
                    Arg.Any<Expression<Func<PendingRequest, PendingRequest>>>(),
                    out Arg.Any<PendingRequest>())
               .Returns(ci =>
                {
                    var predicate = ((Expression<Func<PendingRequest, bool>>)ci[0]).Compile();
                    ci[2] = _pendingRequests.SingleOrDefault(r => predicate.Invoke(r));
                    return ci[2] != null;
                });

            _cache = new PendingRequestCache(responseStore);
            _cache.PeerRatingChanges.Subscribe(change =>
            {
                if (!_reputationByPeerIdentifier.ContainsKey(change.PeerIdentifier)) return;
                _reputationByPeerIdentifier[change.PeerIdentifier] += change.ReputationChange;
            });
        }

        [Fact]
        public void TryMatchResponseAsync_should_match_existing_records_with_matching_correlation_id()
        {
            var responseMatchingIndex1 = new PingResponse()
            {
                CorrelationId = _pendingRequests[1].Content.FromAnySigned<PingRequest>().CorrelationId
            };

            var request = _cache.TryMatchResponse<PingRequest, PingResponse>(responseMatchingIndex1, _peerIds[1]);
            request.Should().NotBeNull();
            request.CorrelationId.Should()
               .Equal(_pendingRequests[1].Content.FromAnySigned<PingResponse>().CorrelationId);
        }

        [Fact]
        public void TryMatchResponseAsync_when_matching_should_increase_reputation()
        {
            var reputationBefore = _reputationByPeerIdentifier[_peerIds[1]];
            TryMatchResponseAsync_should_match_existing_records_with_matching_correlation_id();
            var reputationAfter = _reputationByPeerIdentifier[_peerIds[1]];
            reputationAfter.Should().BeGreaterThan(reputationBefore);

            _reputationByPeerIdentifier.Where(r => !r.Key.Equals(_peerIds[1]))
               .Select(r => r.Value).Should().AllBeEquivalentTo(0);
        }

        [Fact]
        public void TryMatchResponseAsync_should_not_match_existing_records_with_non_matching_correlation_id()
        {
            var responseMatchingNothing = new PingResponse()
            {
                CorrelationId = Guid.NewGuid().ToByteArray().ToByteString()
            };
            var request = _cache.TryMatchResponse<PingRequest, PingResponse>(responseMatchingNothing, _peerIds[1]);
            request.Should().BeNull();
        }

        [Fact]
        public void TryMatchResponseAsync_should_not_match_on_wrong_response_type()
        {
            var matchingRequest = _pendingRequests[1].Content.FromAnySigned<PingRequest>();
            new Action(() => _cache.TryMatchResponse<PingRequest, PingRequest>(matchingRequest, _peerIds[1]))
                .Should().Throw<ArgumentException>();
        }

    }
}
