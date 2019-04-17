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
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Common.UnitTests.Helpers.IO;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Protocol.IPPN;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using PendingRequest = Catalyst.Node.Common.Helpers.IO.Outbound.PendingRequest;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public sealed class MessageCorrelationCacheTests
    {
        private readonly IPeerIdentifier[] _peerIds;
        private readonly IList<PendingRequest> _pendingRequests;
        private readonly P2PCorrelationCache _cache;
        private readonly Dictionary<IPeerIdentifier, int> _reputationByPeerIdentifier;

        public MessageCorrelationCacheTests(ITestOutputHelper output)
        {
            var senderPeerId = PeerIdHelper.GetPeerId("sender");
            _peerIds = new[]
            {
                PeerIdHelper.GetPeerId("abcd"),
                PeerIdHelper.GetPeerId("efgh"),
                PeerIdHelper.GetPeerId("ijkl")
            }.Select(p => new PeerIdentifier(p) as IPeerIdentifier).ToArray();

            _reputationByPeerIdentifier = _peerIds.ToDictionary(p => p, p => 0);
            _pendingRequests = _peerIds.Select((p, i) => new PendingRequest
            {
                Content = new PingRequest().ToAnySigned(senderPeerId, Guid.NewGuid()),
                Recipient = p,
                SentAt = DateTimeOffset.MinValue.Add(TimeSpan.FromMilliseconds(100 * i))
            }).ToList();

            var responseStore = Substitute.For<IMemoryCache>();
            responseStore.TryGetValue(Arg.Any<ByteString>(), out Arg.Any<PendingRequest>())
               .Returns(ci =>
                {
                    output.WriteLine("");
                    ci[1] = _pendingRequests.SingleOrDefault(
                        r => r.Content.CorrelationId.ToBase64() == ((ByteString) ci[0]).ToBase64());
                    return ci[1] != null;
                });

            var logger = Substitute.For<ILogger>();
            _cache = new P2PCorrelationCache(responseStore, logger);
            _cache.PeerRatingChanges.Subscribe(change =>
            {
                if (!_reputationByPeerIdentifier.ContainsKey(change.PeerIdentifier)) return;
                _reputationByPeerIdentifier[change.PeerIdentifier] += change.ReputationChange;
            });
        }

        [Fact]
        public void TryMatchResponseAsync_should_match_existing_records_with_matching_correlation_id()
        {
            var responseMatchingIndex1 = new PingResponse().ToAnySigned(
                _peerIds[1].PeerId,
                _pendingRequests[1].Content.CorrelationId.ToGuid());

            var request = _cache.TryMatchResponse<PingRequest, PingResponse>(responseMatchingIndex1);
            request.Should().NotBeNull();
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
            var responseMatchingNothing = new PingResponse().ToAnySigned(_peerIds[1].PeerId, Guid.NewGuid());
            var request = _cache.TryMatchResponse<PingRequest, PingResponse>(responseMatchingNothing);
            request.Should().BeNull();
        }

        [Fact]
        public void TryMatchResponseAsync_when_not_matching_correlationId_should_not_change_reputation()
        {
            var reputationBefore = _reputationByPeerIdentifier[_peerIds[1]];
            TryMatchResponseAsync_should_not_match_existing_records_with_non_matching_correlation_id();
            var reputationAfter = _reputationByPeerIdentifier[_peerIds[1]];

            _reputationByPeerIdentifier.Select(r => r.Value)
               .Should().AllBeEquivalentTo(0);
        }

        [Fact]
        public void TryMatchResponseAsync_should_not_match_on_wrong_response_type()
        {
            var matchingRequest = _pendingRequests[1].Content;
            new Action(() => _cache.TryMatchResponse<PingRequest, PingRequest>(matchingRequest))
               .Should().Throw<ArgumentException>();
        }
    }
}
