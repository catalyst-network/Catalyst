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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Lib.P2P;
using Microsoft.Extensions.Caching.Memory;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.Deltas
{
    public sealed class DeltaVoterTests : IDisposable
    {
        private IHashProvider _hashProvider;
        private IMemoryCache _cache;
        private IDeltaProducersProvider _producersProvider;
        private DeltaVoter _voter;
        private Cid _previousDeltaHash;
        private IList<PeerId> _producerIds;
        private PeerId _localIdentifier;
        private ILogger _logger;
        private IPeerSettings _peerSettings;

        [SetUp]
        public void Init()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));

            _cache = Substitute.For<IMemoryCache>();

            _previousDeltaHash =
                _hashProvider.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(32)).ToCid();

            _producerIds = "1234"
               .Select((c, i) => PeerIdHelper.GetPeerId(c.ToString()))
               .Shuffle();
            _producersProvider = Substitute.For<IDeltaProducersProvider>();
            _producersProvider.GetDeltaProducersFromPreviousDelta(Arg.Any<Cid>())
               .Returns(_producerIds);

            _localIdentifier = PeerIdHelper.GetPeerId("myself, a producer");
            _peerSettings = _localIdentifier.ToSubstitutedPeerSettings();
            _logger = Substitute.For<ILogger>();
        }

        [Test]
        public void When_candidate_is_null_should_log_and_return_without_hitting_the_cache()
        {
            _voter = new DeltaVoter(_cache, _producersProvider, _peerSettings, _logger);

            _voter.OnNext(null);

            _cache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<object>(), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<object>());
        }

        [Test]
        public void When_candidate_is_empty_should_log_and_return_without_hitting_the_cache()
        {
            _voter = new DeltaVoter(_cache, _producersProvider, _peerSettings, _logger);

            _voter.OnNext(new CandidateDeltaBroadcast());

            _cache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<object>(), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<object>());
        }

        [Test]
        public void When_candidate_producer_id_is_empty_should_log_and_return_without_hitting_the_cache()
        {
            _voter = new DeltaVoter(_cache, _producersProvider, _peerSettings, _logger);

            _voter.OnNext(new CandidateDeltaBroadcast
            {
                Hash = ByteUtil.GenerateRandomByteArray(32).ToByteString(),
                PreviousDeltaDfsHash = ByteUtil.GenerateRandomByteArray(32).ToByteString()
            });

            _cache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<object>(), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<object>());
        }

        [Test]
        public void When_candidate_previous_delta_hash_is_empty_should_log_and_return_without_hitting_the_cache()
        {
            _voter = new DeltaVoter(_cache, _producersProvider, _peerSettings, _logger);

            _voter.OnNext(new CandidateDeltaBroadcast
            {
                Hash = ByteUtil.GenerateRandomByteArray(32).ToByteString(),
                ProducerId = PeerIdHelper.GetPeerId("unknown_producer")
            });

            _cache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<object>(), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<object>());
        }     
        
        [Test]
        public void When_candidate_hash_is_empty_should_log_and_return_without_hitting_the_cache()
        {
            _voter = new DeltaVoter(_cache, _producersProvider, _peerSettings, _logger);

            _voter.OnNext(new CandidateDeltaBroadcast
            {
                PreviousDeltaDfsHash = ByteUtil.GenerateRandomByteArray(32).ToByteString(),
                ProducerId = PeerIdHelper.GetPeerId("unknown_producer")
            });

            _cache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<object>(), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<object>());
        }

        [Test]
        public void When_candidate_is_produced_by_unexpected_producer_should_log_and_return_without_hitting_the_cache()
        {
            var candidateFromUnknownProducer = DeltaHelper.GetCandidateDelta(_hashProvider,
                producerId: PeerIdHelper.GetPeerId("unknown_producer"));

            _voter = new DeltaVoter(_cache, _producersProvider, _peerSettings, _logger);
            _voter.OnNext(candidateFromUnknownProducer);

            _logger.Received(1).Error(Arg.Is<Exception>(e => e is KeyNotFoundException),
                Arg.Any<string>(), Arg.Any<CandidateDeltaBroadcast>());

            _cache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<object>(), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<object>());
        }

        [Test]
        public void When_candidate_not_in_cache_should_build_ScoredCandidate_with_ranking_and_store_it()
        {
            _voter = new DeltaVoter(_cache, _producersProvider, _peerSettings, _logger);

            var candidate = DeltaHelper.GetCandidateDelta(_hashProvider,
                _previousDeltaHash,
                producerId: _producerIds.First());

            var candidateHash = candidate.Hash.ToByteArray().ToCid();

            var addedEntry = Substitute.For<ICacheEntry>();
            _cache.CreateEntry(Arg.Is<string>(s => s.EndsWith(candidateHash)))
               .Returns(addedEntry);

            _voter.OnNext(candidate);

            _cache.Received(1)
               .TryGetValue(Arg.Is<string>(s => s.EndsWith(candidateHash)), out Arg.Any<object>());

            _cache.ReceivedWithAnyArgs(2).CreateEntry(Arg.Any<object>());
            _cache.Received(1).CreateEntry(Arg.Is<string>(s => s.EndsWith(candidateHash)));

            addedEntry.Value.Should().BeAssignableTo<IScoredCandidateDelta>();
            var scoredCandidateDelta = (IScoredCandidateDelta) addedEntry.Value;
            scoredCandidateDelta.Candidate.Hash.SequenceEqual(candidate.Hash).Should().BeTrue();
            scoredCandidateDelta.Score.Should().Be(100 * _producerIds.Count + 1);
        }

        [Test]
        public void When_candidate_in_cache_should_retrieve_ScoredCandidate()
        {
            _voter = new DeltaVoter(_cache, _producersProvider, _peerSettings, _logger);

            var initialScore = 10;
            var cacheCandidate = ScoredCandidateDeltaHelper.GetScoredCandidateDelta(
                _hashProvider,
                producerId: _producerIds.First(),
                previousDeltaHash: _previousDeltaHash,
                score: initialScore);

            var candidateHash = cacheCandidate.Candidate.Hash.ToByteArray().ToCid();

            _cache.TryGetValue(Arg.Any<string>(), out Arg.Any<object>()).Returns(ci =>
            {
                ci[1] = cacheCandidate;
                return true;
            });

            _voter.OnNext(cacheCandidate.Candidate);

            _cache.Received(1)
               .TryGetValue(Arg.Is<string>(s => s.EndsWith(candidateHash)), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<string>());

            cacheCandidate.Score.Should().Be(initialScore + 1);
        }

        [Test]
        public void When_second_candidate_is_more_popular_it_should_score_higher()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                _voter = new DeltaVoter(realCache, _producersProvider, _peerSettings, _logger);

                var firstVotesCount = 10;
                var secondVotesCount = 100 + 100 / 2;

                var retrievedCandidates = AddCandidatesToCacheAndVote(firstVotesCount, secondVotesCount, realCache);

                retrievedCandidates[0].Score.Should().Be(100 * _producerIds.Count + firstVotesCount);
                retrievedCandidates[1].Score.Should().Be(100 * (_producerIds.Count - 1) + secondVotesCount);
            }
        }

        private List<IScoredCandidateDelta> AddCandidatesToCacheAndVote(int firstVotesCount,
            int secondVotesCount,
            MemoryCache realCache)
        {
            var firstCandidate = DeltaHelper.GetCandidateDelta(_hashProvider, _previousDeltaHash,
                producerId: _producerIds.First());

            var secondCandidate = DeltaHelper.GetCandidateDelta(_hashProvider, _previousDeltaHash,
                producerId: _producerIds.Skip(1).First());

            var candidateStream = Enumerable.Repeat(firstCandidate, firstVotesCount)
               .Concat(Enumerable.Repeat(secondCandidate, secondVotesCount))
               .Shuffle().ToObservable();

            using (candidateStream.Subscribe(_voter))
            {
                var firstKey = DeltaVoter.GetCandidateCacheKey(firstCandidate);
                var secondKey = DeltaVoter.GetCandidateCacheKey(secondCandidate);

                realCache.TryGetValue(firstKey, out IScoredCandidateDelta firstRetrieved).Should().BeTrue();
                realCache.TryGetValue(secondKey, out IScoredCandidateDelta secondRetrieved).Should().BeTrue();

                return new List<IScoredCandidateDelta>
                {
                    firstRetrieved, secondRetrieved
                };
            }
        }

        [Test]
        public void When_candidates_not_in_cache_should_create_or_update_a_previous_hash_entry()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                _voter = new DeltaVoter(realCache, _producersProvider, _peerSettings, _logger);

                var candidate1 = DeltaHelper.GetCandidateDelta(
                    _hashProvider,
                    _previousDeltaHash,
                    producerId: _producerIds.First());
                var candidate1CacheKey = DeltaVoter.GetCandidateCacheKey(candidate1);

                var candidate2 = DeltaHelper.GetCandidateDelta(
                    _hashProvider,
                    _previousDeltaHash,
                    producerId: _producerIds.Last());
                var candidate2CacheKey = DeltaVoter.GetCandidateCacheKey(candidate2);

                var previousDeltaCacheKey = DeltaVoter.GetCandidateListCacheKey(candidate1);

                _voter.OnNext(candidate1);

                realCache.TryGetValue(candidate1CacheKey,
                    out ScoredCandidateDelta retrievedCandidate1).Should().BeTrue();
                retrievedCandidate1.Candidate.ProducerId.Should().Be(_producerIds.First());

                realCache.TryGetValue(previousDeltaCacheKey,
                    out ConcurrentBag<string> retrievedCandidateList).Should().BeTrue();
                retrievedCandidateList.Should().BeEquivalentTo(candidate1CacheKey);

                _voter.OnNext(candidate2);

                realCache.TryGetValue(candidate2CacheKey,
                    out ScoredCandidateDelta retrievedCandidate2).Should().BeTrue();
                retrievedCandidate2.Candidate.ProducerId.Should().Be(_producerIds.Last());

                realCache.TryGetValue(previousDeltaCacheKey,
                    out ConcurrentBag<string> retrievedUpdatedCandidateList).Should().BeTrue();
                retrievedUpdatedCandidateList.Should().BeEquivalentTo(candidate1CacheKey, candidate2CacheKey);
            }
        }

        [Test]
        public void GetFavouriteDelta_should_retrieve_favourite_delta()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                _voter = new DeltaVoter(realCache, _producersProvider, _peerSettings, _logger);

                var scoredCandidates = AddCandidatesToCacheAndVote(10, 500, realCache);

                scoredCandidates[1].Score.Should().BeGreaterThan(scoredCandidates[0].Score);

                var previousDeltaHash =
                    scoredCandidates[0].Candidate.PreviousDeltaDfsHash.ToByteArray().ToCid();

                var found = _voter.TryGetFavouriteDelta(previousDeltaHash, out var favouriteCandidate);

                found.Should().BeTrue();

                favouriteCandidate.Candidate.PreviousDeltaDfsHash.ToByteArray()
                   .SequenceEqual(previousDeltaHash.ToArray()).Should().BeTrue();
                favouriteCandidate.Candidate.Hash.ToByteArray()
                   .SequenceEqual(scoredCandidates[1].Candidate.Hash.ToByteArray()).Should().BeTrue();
                favouriteCandidate.Candidate.ProducerId.Should().Be(scoredCandidates[1].Candidate.ProducerId);
            }
        }

        [Test]
        public void GetFavouriteDelta_should_return_null_on_unknown_previous_delta_hash()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                _voter = new DeltaVoter(realCache, _producersProvider, _peerSettings, _logger);

                AddCandidatesToCacheAndVote(10, 500, realCache);

                var found = _voter.TryGetFavouriteDelta(
                    _hashProvider.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(32)).ToCid(),
                    out var favouriteCandidate);

                found.Should().BeFalse();
                favouriteCandidate.Should().BeNull();
            }
        }

        [Test]
        public void GetFavouriteDelta_should_return_lowest_hash_when_candidate_scores_are_equal()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                _voter = new DeltaVoter(realCache, _producersProvider, _peerSettings, _logger);

                var scoredCandidates = AddCandidatesToCacheAndVote(10, 110, realCache);

                scoredCandidates.Select(c => c.Score).Distinct().Count().Should().Be(1);
                scoredCandidates.Select(c => c.Candidate.Hash).Distinct().Count().Should().Be(2);
                scoredCandidates.Select(c => c.Candidate.PreviousDeltaDfsHash).Distinct().Count().Should().Be(1);

                var found = _voter.TryGetFavouriteDelta(
                    scoredCandidates.First().Candidate.PreviousDeltaDfsHash.ToByteArray().ToCid(),
                    out var favouriteCandidate);

                found.Should().BeTrue();
                var expectedFavourite = scoredCandidates
                   .OrderBy(c => c.Candidate.Hash.ToByteArray(), ByteUtil.ByteListMinSizeComparer.Default)
                   .First();

                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                favouriteCandidate.Candidate.Hash.Equals(expectedFavourite.Candidate.Hash);
            }
        }

        public void Dispose() { _cache?.Dispose(); }
    }
}
