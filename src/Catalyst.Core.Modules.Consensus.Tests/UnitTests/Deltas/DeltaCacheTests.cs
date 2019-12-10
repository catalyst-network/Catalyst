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

using System.Collections.Generic;
using System.Linq;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using FluentAssertions;
using Lib.P2P;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.Deltas
{
    public class DeltaCacheTests
    {
        private readonly IHashProvider _hashProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly IDeltaDfsReader _dfsReader;
        private readonly DeltaCache _deltaCache;
        private readonly ILogger _logger;

        public DeltaCacheTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            _memoryCache = Substitute.For<IMemoryCache>();
            _dfsReader = Substitute.For<IDeltaDfsReader>();
            _logger = Substitute.For<ILogger>();

            var tokenProvider = Substitute.For<IDeltaCacheChangeTokenProvider>();
            tokenProvider.GetChangeToken().Returns(Substitute.For<IChangeToken>());

            _deltaCache = new DeltaCache(_hashProvider, _memoryCache, _dfsReader, tokenProvider, _logger);
        }

        [Fact]
        public void Genesis_Hash_Should_Be_Always_Present()
        {
            _memoryCache.Received().CreateEntry(_deltaCache.GenesisHash);
        }

        [Fact]
        public void TryGetDelta_Should_Not_Hit_The_Dfs_Or_Store_Delta_When_Delta_Is_In_Cache()
        {
            _memoryCache.ClearReceivedCalls(); // needed because of the CreateEntry call from the DeltaCache .ctor
            var deltaFromCache = DeltaHelper.GetDelta(_hashProvider);
            var cid = _hashProvider.ComputeUtf8MultiHash("abc").ToCid();

            _memoryCache.TryGetValue(Arg.Is(cid), out Arg.Any<Delta>())
               .Returns(ci =>
                {
                    ci[1] = deltaFromCache;
                    return true;
                });

            var found = _deltaCache.TryGetOrAddConfirmedDelta(cid, out var delta);

            delta.Should().Be(deltaFromCache);
            found.Should().BeTrue();

            _dfsReader.DidNotReceiveWithAnyArgs().TryReadDeltaFromDfs(default, out _);
            _memoryCache.DidNotReceiveWithAnyArgs().CreateEntry(default);
        }

        [Fact]
        public void TryGetDelta_Should_Hit_The_Dfs_When_Delta_Is_Not_In_Cache()
        {
            var deltaFromDfs = DeltaHelper.GetDelta(_hashProvider);

            var cid = _hashProvider.ComputeUtf8MultiHash("def").ToCid();
            ExpectDeltaFromDfsAndNotFromCache(cid, deltaFromDfs);

            var cacheEntry = Substitute.For<ICacheEntry>();

            _memoryCache.CreateEntry(cid).Returns(cacheEntry);

            var found = _deltaCache.TryGetOrAddConfirmedDelta(cid, out var delta);

            delta.Should().Be(deltaFromDfs);
            found.Should().BeTrue();

            _memoryCache.Received(1).TryGetValue(cid, out Arg.Any<Delta>());
            _dfsReader.Received(1).TryReadDeltaFromDfs(cid, out Arg.Any<Delta>());
        }

        [Fact]
        public void TryGetDelta_Should_Cache_Delta_With_Expiry_Options_When_Delta_Is_Not_In_Cache()
        {
            var deltaFromDfs = DeltaHelper.GetDelta(_hashProvider);
            var cid = _hashProvider.ComputeUtf8MultiHash("ijk").ToCid();
            ExpectDeltaFromDfsAndNotFromCache(cid, deltaFromDfs);

            var cacheEntry = Substitute.For<ICacheEntry>();
            var expirationTokens = new List<IChangeToken>();
            cacheEntry.ExpirationTokens.Returns(expirationTokens);
            var expirationCallbacks = new List<PostEvictionCallbackRegistration>();
            cacheEntry.PostEvictionCallbacks.Returns(expirationCallbacks);

            _memoryCache.CreateEntry(cid).Returns(cacheEntry);

            _deltaCache.TryGetOrAddConfirmedDelta(cid, out _);

            _memoryCache.Received(1).CreateEntry(cid);
            cacheEntry.Value.Should().Be(deltaFromDfs);

            cacheEntry.ExpirationTokens.Count.Should().Be(1);
            cacheEntry.PostEvictionCallbacks.Count.Should().Be(1);

            _logger.ClearReceivedCalls();
            cacheEntry.PostEvictionCallbacks.Single().EvictionCallback
               .Invoke("key", "value", EvictionReason.Expired, "state");
            _logger.Received(1).Debug(Arg.Any<string>(), Arg.Any<object>());
        }

        private void ExpectDeltaFromDfsAndNotFromCache(Cid cid, Delta deltaFromDfs)
        {
            _memoryCache.TryGetValue(Arg.Is(cid), out Arg.Any<Delta>())
               .Returns(false);
            _dfsReader.TryReadDeltaFromDfs(Arg.Is(cid), out Arg.Any<Delta>())
               .Returns(ci =>
                {
                    ci[1] = deltaFromDfs;
                    return true;
                });
        }
    }
}
