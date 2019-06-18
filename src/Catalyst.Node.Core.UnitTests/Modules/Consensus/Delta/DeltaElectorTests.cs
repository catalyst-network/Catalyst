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
using Catalyst.Common.Util;
using Catalyst.Node.Core.Modules.Consensus.Delta;
using Catalyst.Protocol.Delta;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus.Delta
{
    public class BadFavouritesData : TheoryData<FavouriteDeltaBroadcast>
    {
        public BadFavouritesData()
        {
            Add(null);
            Add(new FavouriteDeltaBroadcast());
            Add(new FavouriteDeltaBroadcast
            {
                Candidate = new CandidateDeltaBroadcast
                {
                    Hash = ByteUtil.GenerateRandomByteArray(32).ToByteString(),
                    ProducerId = PeerIdHelper.GetPeerId("unknown_producer")
                },
                VoterId = PeerIdHelper.GetPeerId("candidate field is invalid")
            });
            Add(new FavouriteDeltaBroadcast
            {
                Candidate = DeltaHelper.GetCandidateDelta()
            });
        }
    }

    public class DeltaElectorTests
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;

        public DeltaElectorTests()
        {
            _logger = Substitute.For<ILogger>();
            _cache = Substitute.For<IMemoryCache>();
        }

        [Theory]
        [ClassData(typeof(BadFavouritesData))]
        public void When_receiving_bad_favourite_should_log_and_not_hit_the_cache(FavouriteDeltaBroadcast badFavourite)
        {
            var elector = new DeltaElector(_cache, _logger);

            elector.OnNext(badFavourite);

            _logger.Received(1).Error(Arg.Is<Exception>(e => e is ArgumentException),
                Arg.Any<string>(), Arg.Any<string>());

            _cache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<object>(), out Arg.Any<object>());
            _cache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<object>());
        }

        [Fact]
        public void When_receiving_new_valid_favourite_should_store_in_cache()
        {
            var favourite = FavouriteDeltaHelper.GetFavouriteDelta();
            var candidateListKey = DeltaElector.GetCandidateListCacheKey(favourite);
            var elector = new DeltaElector(_cache, _logger);

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

        [Fact]
        public void When_receiving_known_favourite_should_not_store_in_cache()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                var favourite = FavouriteDeltaHelper.GetFavouriteDelta();
                var candidateListKey = DeltaElector.GetCandidateListCacheKey(favourite);

                var elector = new DeltaElector(realCache, _logger);

                elector.OnNext(favourite);
                elector.OnNext(favourite);

                realCache.TryGetValue(candidateListKey, out IDictionary<FavouriteDeltaBroadcast, bool> retrieved)
                   .Should().BeTrue();

                retrieved.Keys.Count.Should().Be(1);
                retrieved.Should().ContainKey(favourite);
            }
        }

        [Fact]
        public void When_favourite_has_different_producer_it_should_not_create_duplicate_entries()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                var producers = "abc".Select(c => PeerIdHelper.GetPeerId(c.ToString()))
                   .ToArray();
                var hashProduced = ByteUtil.GenerateRandomByteArray(32);
                var previousHash = ByteUtil.GenerateRandomByteArray(32);
                var candidates = producers.Select((p, i) =>
                    DeltaHelper.GetCandidateDelta(previousHash, hashProduced, producers[i])
                ).ToArray();

                var votersCount = 4;
                var favourites = Enumerable.Repeat(candidates[0], 5)
                   .Concat(Enumerable.Repeat(candidates[1], 2))
                   .Concat(Enumerable.Repeat(candidates[2], 8))
                   .Select((c, j) => new FavouriteDeltaBroadcast
                    {
                        Candidate = c,
                        VoterId = PeerIdHelper.GetPeerId((j % votersCount).ToString())
                    }).ToArray();

                var elector = new DeltaElector(realCache, _logger);

                var favouriteStream = favourites.ToObservable();
                using (var subscription = favouriteStream.Subscribe(elector))
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

        [Fact]
        public void GetMostPopularCandidateDelta_should_return_the_favourite_with_most_voter_ids()
        {
            using (var realCache = new MemoryCache(new MemoryCacheOptions()))
            {
                var producers = "ab".Select(c => PeerIdHelper.GetPeerId(c.ToString()))
                   .ToArray();
                var previousHash = ByteUtil.GenerateRandomByteArray(32);
                var candidates = producers.Select((p, i) =>
                    DeltaHelper.GetCandidateDelta(previousHash,
                        ByteUtil.GenerateRandomByteArray(32),
                        producers[i])
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
                    }).ToArray();

                var elector = new DeltaElector(realCache, _logger);

                var favouriteStream = favourites.ToObservable();
                using (var subscription = favouriteStream.Subscribe(elector))
                {
                    var candidateListKey = DeltaElector.GetCandidateListCacheKey(favourites.First());

                    realCache.TryGetValue(candidateListKey, out IDictionary<FavouriteDeltaBroadcast, bool> retrieved)
                       .Should().BeTrue();

                    retrieved.Keys.Count.Should().Be(secondVoteCount + firstVotesCount,
                        "all these favourites are being voted for by different peers.");
                }
            }
        }

        [Fact]
        public void GetMostPopularCandidateDelta_should_return_null_on_unknown_previous_delta_hash()
        {
            _cache.TryGetValue(Arg.Any<object>(), out Arg.Any<object>()).Returns(false);

            var elector = new DeltaElector(_cache, _logger);
            
            var popular = elector.GetMostPopularCandidateDelta(ByteUtil.GenerateRandomByteArray(32));

            popular.Should().BeNull();
            _logger.Received(1).Debug(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
