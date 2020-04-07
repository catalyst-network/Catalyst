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
using System.Reactive.Linq;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Lib.P2P.ReputationSystem;
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
using Microsoft.Reactive.Testing;
using MultiFormats.Registry;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using SharpRepository.InMemoryRepository;
using Peer = Catalyst.Core.Lib.P2P.Models.Peer;
using Catalyst.Core.Lib.P2P.Repository;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.Deltas
{
    public class BadFavouritesData : List<object[]>
    {
        public BadFavouritesData()
        {
            var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));

            Add(new object[] { null, typeof(ArgumentNullException) });
            Add(new object[] { new FavouriteDeltaBroadcast(), typeof(InvalidDataException) });
            Add(new object[] { new FavouriteDeltaBroadcast
            {
                Candidate = new CandidateDeltaBroadcast
                {
                    Hash = ByteUtil.GenerateRandomByteArray(32).ToByteString(),
                    ProducerId = PeerIdHelper.GetPeerId("unknown_producer")
                },
                VoterId = PeerIdHelper.GetPeerId("candidate field is invalid")
            }, typeof(InvalidDataException) });
            Add(new object[] { new FavouriteDeltaBroadcast
            {
                Candidate = DeltaHelper.GetCandidateDelta(hashProvider)
            }, typeof(InvalidDataException) });
        }
    }

    public class DeltaElectorTests
    {
        private TestScheduler _testScheduler;
        private ILogger _logger;
        private IReputationManager _reputationManager;
        private IHashProvider _hashProvider;
        private IMemoryCache _cache;
        private IDeltaProducersProvider _deltaProducersProvider;

        [SetUp]
        public void Init()
        {
            _testScheduler = new TestScheduler();
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            _logger = Substitute.For<ILogger>();
            _reputationManager =
                new ReputationManager(new PeerRepository(new InMemoryRepository<Peer, string>()), _logger, _testScheduler);
            _cache = Substitute.For<IMemoryCache>();
            _deltaProducersProvider = Substitute.For<IDeltaProducersProvider>();
        }

        [TestCaseSource(typeof(BadFavouritesData))]
        public void When_receiving_bad_favourite_should_log_and_not_hit_the_cache(FavouriteDeltaBroadcast badFavourite,
            Type exceptionType)
        {
            var elector = new DeltaElector(_cache, _deltaProducersProvider, _reputationManager, _logger);

            elector.OnNext(badFavourite);

            _cache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<object>(), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<object>());
        }

        [Test]
        public void When_receiving_new_valid_favourite_should_store_in_cache()
        {
            var favourite = DeltaHelper.GetFavouriteDelta(_hashProvider);
            var candidateListKey = DeltaElector.GetCandidateListCacheKey(favourite);

            AddVoterAsExpectedProducer(favourite.VoterId);

            var elector = new DeltaElector(_cache, _deltaProducersProvider, _reputationManager, _logger);

            var addedEntry = Substitute.For<ICacheEntry>();
            _cache.CreateEntry(Arg.Is<string>(s => s.Equals(candidateListKey)))
               .Returns(addedEntry);

            elector.OnNext(favourite);

            _cache.Received(1).TryGetValue(Arg.Is<string>(s => s.Equals(candidateListKey)), out Arg.Any<object>());
            _cache.Received(1).CreateEntry(Arg.Is<string>(s => s.Equals(candidateListKey)));

            var addedValue = addedEntry.Value;
            addedValue.Should().BeAssignableTo<IDictionary<FavouriteDeltaBroadcast, bool>>();
            ((IDictionary<FavouriteDeltaBroadcast, bool>) addedValue).Should().ContainKey(favourite);
        }

        [Test]
        public void When_receiving_known_favourite_should_not_store_in_cache()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                var favourite = DeltaHelper.GetFavouriteDelta(_hashProvider);
                var candidateListKey = DeltaElector.GetCandidateListCacheKey(favourite);

                AddVoterAsExpectedProducer(favourite.VoterId);

                var elector = new DeltaElector(realCache, _deltaProducersProvider, _reputationManager, _logger);

                elector.OnNext(favourite);
                elector.OnNext(favourite);

                realCache.TryGetValue(candidateListKey, out IDictionary<FavouriteDeltaBroadcast, bool> retrieved)
                   .Should().BeTrue();

                retrieved.Keys.Count.Should().Be(1);
                retrieved.Should().ContainKey(favourite);
            }
        }

        [Test]
        public void When_voter_not_a_producer_should_not_save_vote()
        {
            var favourite = DeltaHelper.GetFavouriteDelta(_hashProvider);

            _deltaProducersProvider
               .GetDeltaProducersFromPreviousDelta(Arg.Any<Cid>())
               .Returns(new List<PeerId> { PeerIdHelper.GetPeerId("the only known producer") });

            var elector = new DeltaElector(_cache, _deltaProducersProvider, _reputationManager, _logger);

            elector.OnNext(favourite);

            _deltaProducersProvider.Received(1)
               .GetDeltaProducersFromPreviousDelta(Arg.Is<Cid>(h =>
                    favourite.Candidate.PreviousDeltaDfsHash.Equals(h.ToArray().ToByteString())));
            _cache.DidNotReceiveWithAnyArgs().TryGetValue(default, out _);
        }

        [Test]
        public void Should_DeRep_Peers_That_Vote_When_They_Are_Not_Delta_Producers()
        {
            var favourite = DeltaHelper.GetFavouriteDelta(_hashProvider);

            var peer = new Peer { Reputation = 100, PeerId = favourite.VoterId };
            _reputationManager.PeerRepository.Add(new Peer { Reputation = 100, PeerId = favourite.VoterId });

            var expectedReputation = peer.Reputation + ReputationEventType.VoterIsNotProducer.Amount;

            _deltaProducersProvider
               .GetDeltaProducersFromPreviousDelta(Arg.Any<Cid>())
               .Returns(new List<PeerId>());

            var elector = new DeltaElector(_cache, _deltaProducersProvider, _reputationManager, _logger);

            elector.OnNext(favourite);

            _testScheduler.Start();

            var updatedPeer = _reputationManager.PeerRepository.Get(favourite.VoterId);
            expectedReputation.Should().Be(updatedPeer.Reputation);

            _cache.DidNotReceiveWithAnyArgs().TryGetValue(default, out _);
        }

        [Test]
        public void When_favourite_has_different_producer_it_should_not_create_duplicate_entries()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                var producers = "abc".Select(c => PeerIdHelper.GetPeerId(c.ToString()))
                   .ToArray();
                var hashProduced = _hashProvider.ComputeUtf8MultiHash("newHash").ToCid();
                var previousHash = _hashProvider.ComputeUtf8MultiHash("prevHash").ToCid();
                var candidates = producers.Select((p, i) =>
                    DeltaHelper.GetCandidateDelta(_hashProvider, previousHash, hashProduced, producers[i])
                ).ToArray();

                var votersCount = 4;
                var favourites = Enumerable.Repeat(candidates[0], 5)
                   .Concat(Enumerable.Repeat(candidates[1], 2))
                   .Concat(Enumerable.Repeat(candidates[2], 8))
                   .Select((c, j) => new FavouriteDeltaBroadcast
                   {
                       Candidate = c,
                       VoterId = PeerIdHelper.GetPeerId((j % votersCount).ToString())
                   }).ToList();

                AddVoterAsExpectedProducer(favourites.Select(f => f.VoterId).ToArray());

                var elector = new DeltaElector(realCache, _deltaProducersProvider, _reputationManager, _logger);

                var favouriteStream = favourites.ToObservable();
                using (favouriteStream.Subscribe(elector))
                {
                    var candidateListKey = DeltaElector.GetCandidateListCacheKey(favourites.First());

                    realCache.TryGetValue(candidateListKey, out IDictionary<FavouriteDeltaBroadcast, bool> retrieved)
                       .Should().BeTrue();

                    retrieved.Keys.Count.Should().Be(votersCount,
                        $"all these favourites are giving the same hash, with only {votersCount} different voters " +
                        $"overall, this should result in only {votersCount} new entries if we don't want to double count");
                }
            }
        }

        [Test]
        public void GetMostPopularCandidateDelta_should_return_the_favourite_with_most_voter_ids()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                var producers = "ab".Select(c => PeerIdHelper.GetPeerId(c.ToString()))
                   .ToArray();
                var previousHash = _hashProvider.ComputeUtf8MultiHash("previousHash").ToCid();
                var newHash = _hashProvider.ComputeUtf8MultiHash("newHash").ToCid();
                var candidates = producers.Select((p, i) =>
                    DeltaHelper.GetCandidateDelta(_hashProvider, previousHash, newHash, producers[i])
                ).ToArray();

                var firstVotesCount = 41;
                var secondVoteCount = 119;
                var favourites = Enumerable.Repeat(candidates[0], firstVotesCount)
                   .Concat(Enumerable.Repeat(candidates[1], secondVoteCount))
                   .Shuffle()
                   .Select((c, j) => new FavouriteDeltaBroadcast
                   {
                       Candidate = c,
                       VoterId = PeerIdHelper.GetPeerId(j.ToString())
                   }).ToList();

                AddVoterAsExpectedProducer(favourites.Select(f => f.VoterId).ToArray());

                var elector = new DeltaElector(realCache, _deltaProducersProvider, _reputationManager, _logger);

                var favouriteStream = favourites.ToObservable();
                using (favouriteStream.Subscribe(elector))
                {
                    var candidateListKey = DeltaElector.GetCandidateListCacheKey(favourites.First());

                    realCache.TryGetValue(candidateListKey, out IDictionary<FavouriteDeltaBroadcast, bool> retrieved)
                       .Should().BeTrue();

                    retrieved.Keys.Count.Should().Be(secondVoteCount + firstVotesCount,
                        "all these favourites are being voted for by different peers.");
                }
            }
        }

        [Test]
        public void GetMostPopularCandidateDelta_should_return_null_on_unknown_previous_delta_hash()
        {
            _cache.TryGetValue(Arg.Any<object>(), out Arg.Any<object>()).Returns(false);

            var elector = new DeltaElector(_cache, _deltaProducersProvider, _reputationManager, _logger);

            var previousHash = _hashProvider.ComputeUtf8MultiHash("previous").ToCid();

            var popular = elector.GetMostPopularCandidateDelta(previousHash);

            popular.Should().BeNull();
            _logger.Received(1).Debug(Arg.Any<string>(), Arg.Any<Cid>());
        }

        private void AddVoterAsExpectedProducer(params PeerId[] producers)
        {
            _deltaProducersProvider
               .GetDeltaProducersFromPreviousDelta(Arg.Any<Cid>())
               .Returns(producers.ToList());
        }
    }
}
