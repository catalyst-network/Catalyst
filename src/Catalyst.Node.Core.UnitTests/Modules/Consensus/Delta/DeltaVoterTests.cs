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
using System.Reactive.Linq;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Consensus.Delta;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Util;
using Catalyst.Node.Core.Modules.Consensus.Delta;
using Catalyst.Node.Core.UnitTests.TestUtils;
using Catalyst.Protocol.Delta;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Nethereum.Hex.HexConvertors.Extensions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus.Delta
{
    public sealed class DeltaVoterTests : IDisposable
    {
        public static readonly List<object[]> DodgyCandidates;

        private readonly IMemoryCache _cache;
        private readonly IDeltaProducersProvider _producersProvider;
        private DeltaVoter _voter;
        private readonly byte[] _previousDeltaHash;
        private readonly IList<IPeerIdentifier> _producerIds;

        static DeltaVoterTests()
        {
            DodgyCandidates = new List<object[]>
            {
                new object[] {null},
                new object[] {new CandidateDelta()},
                new object[] 
                {
                    new CandidateDelta
                    {
                        Hash = ByteUtil.GenerateRandomByteArray(32).ToByteString(),
                        PreviousDeltaDfsHash = ByteUtil.GenerateRandomByteArray(32).ToByteString()
                    }
                },
                new object[]
                {
                    new CandidateDelta
                    {
                        Hash = ByteUtil.GenerateRandomByteArray(32).ToByteString(),
                        ProducerId = PeerIdHelper.GetPeerId("unknown_producer")
                    }
                },
                new object[]
                {
                    new CandidateDelta
                    {
                        PreviousDeltaDfsHash = ByteUtil.GenerateRandomByteArray(32).ToByteString(),
                        ProducerId = PeerIdHelper.GetPeerId("unknown_producer")
                    }
                },
            };
        }

        public DeltaVoterTests()
        {
            _cache = Substitute.For<IMemoryCache>();

            _previousDeltaHash = ByteUtil.GenerateRandomByteArray(32);

            _producerIds = "1234"
               .Select((c, i) => PeerIdentifierHelper.GetPeerIdentifier(c.ToString(), clientVersion: i))
               .Shuffle();
            _producersProvider = Substitute.For<IDeltaProducersProvider>();
            _producersProvider.GetDeltaProducersFromPreviousDelta(Arg.Any<byte[]>())
               .Returns(_producerIds);
        }

        [Theory]
        [MemberData(nameof(DodgyCandidates))]
        public void When_candidate_is_dodgy_should_log_and_return_without_hitting_the_cache(CandidateDelta dodgyCandidate)
        {
            var logger = Substitute.For<ILogger>();
            _voter = new DeltaVoter(_cache, _producersProvider, logger);

            _voter.OnNext(dodgyCandidate);

            logger.Received(1).Error(Arg.Is<Exception>(e => e is ArgumentException),
                Arg.Any<string>(), Arg.Any<string>());

            _cache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<object>(), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<object>());
        }

        [Fact]
        public void When_candidate_is_produced_by_unexpected_producer_should_log_and_return_without_hitting_the_cache()
        {
            var candidateFromUnknownProducer = CandidateDeltaHelper.GetCandidateDelta(
                producerId: PeerIdHelper.GetPeerId("unknown_producer"));
            var logger = Substitute.For<ILogger>();

            _voter = new DeltaVoter(_cache, _producersProvider, logger);
            _voter.OnNext(candidateFromUnknownProducer);

            logger.Received(1).Error(Arg.Is<Exception>(e => e is KeyNotFoundException),
                Arg.Any<string>(), Arg.Any<string>());

            _cache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<object>(), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<object>());
        }

        [Fact]
        public void When_candidate_not_in_cache_should_build_ScoredCandidate_with_ranking_and_store_it()
        {
            _voter = new DeltaVoter(_cache, _producersProvider, Substitute.For<ILogger>());

            var candidate = CandidateDeltaHelper.GetCandidateDelta(
                previousDeltaHash: _previousDeltaHash,
                producerId: _producerIds.First().PeerId);

            var candidateHashAsHex = candidate.Hash.ToByteArray().ToHex();

            var addedEntry = Substitute.For<ICacheEntry>();
            _cache.CreateEntry(Arg.Is<string>(s => s.EndsWith(candidateHashAsHex)))
               .Returns(addedEntry);

            _voter.OnNext(candidate);

            _cache.Received(1).TryGetValue(Arg.Is<string>(s => s.EndsWith(candidateHashAsHex)), out Arg.Any<object>());
            _cache.Received(1).CreateEntry(Arg.Is<string>(s => s.EndsWith(candidateHashAsHex)));
            addedEntry.Value.Should().BeAssignableTo<IScoredCandidateDelta>();
            var scoredCandidateDelta = (IScoredCandidateDelta) addedEntry.Value;
            scoredCandidateDelta.Candidate.Hash.SequenceEqual(candidate.Hash).Should().BeTrue();
            scoredCandidateDelta.Score.Should().Be(100 * _producerIds.Count + 1);
        }

        [Fact]
        public void When_candidate_in_cache_should_retrieve_ScoredCandidate()
        {
            _voter = new DeltaVoter(_cache, _producersProvider, Substitute.For<ILogger>());

            var initialScore = 10;
            var cacheCandidate = ScoredCandidateDeltaHelper.GetScoredCandidateDelta(
                producerId: _producerIds.First().PeerId,
                previousDeltaHash: _previousDeltaHash,
                score: initialScore);

            var candidateHashAsHex = cacheCandidate.Candidate.Hash.ToByteArray().ToHex();

            _cache.TryGetValue(Arg.Any<string>(), out Arg.Any<object>()).Returns(ci =>
            {
                ci[1] = cacheCandidate;
                return true;
            });

            _voter.OnNext(cacheCandidate.Candidate);

            _cache.Received(1).TryGetValue(Arg.Is<string>(s => s.EndsWith(candidateHashAsHex)), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<string>());

            cacheCandidate.Score.Should().Be(initialScore + 1);
        }

        [Fact]
        public void When_second_candidate_is_more_popular_it_should_score_higher()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                _voter = new DeltaVoter(realCache, _producersProvider, Substitute.For<ILogger>());

                var topCandidate = CandidateDeltaHelper.GetCandidateDelta(_previousDeltaHash,
                    producerId: _producerIds.First().PeerId);

                var secondCandidate = CandidateDeltaHelper.GetCandidateDelta(_previousDeltaHash,
                    producerId: _producerIds.Skip(1).First().PeerId);

                var firstVotesCount = 10;
                var secondVotesCount = 100 + 100 / 2;
                var candidateStream = Enumerable.Repeat(topCandidate, firstVotesCount)
                   .Concat(Enumerable.Repeat(secondCandidate, secondVotesCount))
                   .Shuffle().ToObservable();

                candidateStream.Subscribe(_voter);

                var firstKey = DeltaVoter.GetCacheKey(topCandidate.Hash.ToByteArray().ToHex());
                var secondKey = DeltaVoter.GetCacheKey(secondCandidate.Hash.ToByteArray().ToHex());

                realCache.TryGetValue(firstKey, out IScoredCandidateDelta firstRetrieved).Should().BeTrue();
                realCache.TryGetValue(secondKey, out IScoredCandidateDelta secondRetrieved).Should().BeTrue();

                firstRetrieved.Score.Should().Be(100 * _producerIds.Count + firstVotesCount);
                secondRetrieved.Score.Should().Be(100 * (_producerIds.Count - 1) + secondVotesCount);
            }
        }

        public void Dispose()
        {
            _cache?.Dispose();
        }
    }
}
