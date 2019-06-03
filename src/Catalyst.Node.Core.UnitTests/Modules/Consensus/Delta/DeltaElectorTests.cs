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
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Util;
using Catalyst.Node.Core.Modules.Consensus.Delta;
using Catalyst.Protocol.Delta;
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
                Candidate = CandidateDeltaHelper.GetCandidateDelta()
            });
        }
    }

    public class DeltaElectorTests
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _popularityCache;
        
        public DeltaElectorTests()
        {
            _logger = Substitute.For<ILogger>();
            _popularityCache = Substitute.For<IMemoryCache>();
        }

        [Theory]
        [ClassData(typeof(BadFavouritesData))]
        public void When_receiving_bad_favourite_should_log_and_not_hit_the_cache(FavouriteDeltaBroadcast badFavourite)
        {
            var elector = new DeltaElector(_popularityCache, _logger);

            elector.OnNext(badFavourite);

            _logger.Received(1).Error(Arg.Is<Exception>(e => e is ArgumentException),
                Arg.Any<string>(), Arg.Any<string>());

            _popularityCache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<object>(), out Arg.Any<object>());
            _popularityCache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<object>());
        }

        [Fact]
        public void When_receiving_valid_favourite_should_store_in_cache()
        {
            var favourite = FavouriteDeltaHelper.GetFavouriteDelta();
            var elector = new DeltaElector(_popularityCache, _logger);

            elector.OnNext(favourite);

            var cacheKey = DeltaElector.GetCandidateListCacheKey(favourite);

            _popularityCache.Received(1).TryGetValue(Arg.Is<string>(s => s.Equals(cacheKey)), out Arg.Any<object>());
            _popularityCache.Received(1).CreateEntry(Arg.Is<string>(s => s.Equals(cacheKey)));
        }
    }
}
