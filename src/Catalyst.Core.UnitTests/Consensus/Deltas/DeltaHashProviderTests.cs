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
using System.Reflection;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Core.Consensus.Deltas;
using Catalyst.Core.Extensions;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.UnitTests.Consensus.Deltas
{
    public sealed class DeltaHashProviderTests : SelfAwareTestBase
    {
        //we just need an offset to not have TimeStamp = 0 when building deltas (cf DeltaHelper)
        private const int Offset = 100;
        private readonly IDeltaCache _deltaCache;
        private readonly ILogger _logger;

        public DeltaHashProviderTests(ITestOutputHelper output) : base(output)
        {
            _deltaCache = Substitute.For<IDeltaCache>();
            _logger = new LoggerConfiguration()
               .MinimumLevel.Verbose()
               .WriteTo.TestOutput(output)
               .CreateLogger()
               .ForContext(MethodBase.GetCurrentMethod().DeclaringType);

            _deltaCache.GenesisAddress.Returns(DeltaCache.GenesisHash.AsBase32Address());
        }

        [Fact]
        public void Generate_Genesis_Hash()
        {
            var emptyDelta = new Delta();
            var hash = emptyDelta.ToByteArray().ComputeMultihash(new BLAKE2B_256());
            var dfsAddress = hash.AsBase32Address();

            Output.WriteLine(dfsAddress);
        }

        [Fact]
        public void TryUpdateLatestHash_Should_Update_If_Hashes_Are_Valid()
        {
            const int deltaCount = 2;
            BuildDeltasAndSetCacheExpectations(deltaCount);

            var hashProvider = new DeltaHashProvider(_deltaCache, _logger, 3);
            var updated = hashProvider.TryUpdateLatestHash(GetHash(0), GetHash(1));
            updated.Should().BeTrue();

            hashProvider.GetLatestDeltaHash()
               .Should().Be(GetHash(1));
        }

        [Fact]
        public void TryUpdateLatestHash_Should_Push_New_Hash_On_Stream_When_Updating_Latest()
        {
            const int deltaCount = 2;
            BuildDeltasAndSetCacheExpectations(deltaCount);
            var observer = Substitute.For<IObserver<Multihash>>();

            var hashProvider = new DeltaHashProvider(_deltaCache, _logger, 3);

            using (hashProvider.DeltaHashUpdates.Subscribe(observer))
            {
                hashProvider.TryUpdateLatestHash(GetHash(0), GetHash(1));

                observer.Received(1).OnNext(Arg.Is(GetHash(1)));
            }
        }

        [Fact]
        public void TryUpdateLatestHash_Should_Put_New_Hash_At_The_Top_Of_The_List()
        {
            const int deltaCount = 3;
            BuildDeltasAndSetCacheExpectations(deltaCount);

            var hashProvider = new DeltaHashProvider(_deltaCache, _logger, 4);
            var updated = hashProvider.TryUpdateLatestHash(GetHash(0), GetHash(1));
            updated.Should().BeTrue();
            updated = hashProvider.TryUpdateLatestHash(GetHash(1), GetHash(2));
            updated.Should().BeTrue();

            hashProvider.GetLatestDeltaHash()
               .Should().Be(GetHash(2));
        }

        [Fact]
        public void DeltaHashProviderConstructor_Should_Apply_Capacity()
        {
            const int deltaCount = 9;
            BuildDeltasAndSetCacheExpectations(deltaCount);

            const int cacheCapacity = 3;
            var hashProvider = new DeltaHashProvider(_deltaCache, _logger, cacheCapacity);

            Enumerable.Range(1, deltaCount - 1).ToList().ForEach(i =>
            {
                var updated = hashProvider.TryUpdateLatestHash(GetHash(i - 1), GetHash(i));
                updated.Should().BeTrue();
            });

            hashProvider.GetLatestDeltaHash().Should().Be(GetHash(deltaCount - 1));

            var evictedCount = deltaCount - cacheCapacity;
            var nonEvictedRange = Enumerable.Range(evictedCount, deltaCount - evictedCount);
            nonEvictedRange.ToList().ForEach(i =>
            {
                hashProvider.GetLatestDeltaHash(GetDateTimeForIndex(i))
                   .Should().Be(GetHash(i));
            });

            var evictedRange = Enumerable.Range(0, evictedCount);
            evictedRange.ToList().ForEach(i =>
            {
                hashProvider.GetLatestDeltaHash(GetDateTimeForIndex(i))
                   .Should().Be(default);
            });
        }

        private DateTime GetDateTimeForIndex(int i)
        {
            return DateTime.FromOADate(Offset + i).ToUniversalTime();
        }

        private Multihash GetHash(int i)
        {
            var hash = BitConverter.GetBytes(i).ComputeMultihash(new BLAKE2B_256());
            return hash;
        }

        private void BuildDeltasAndSetCacheExpectations(int deltaCount)
        {
            var deltas = Enumerable.Range(0, deltaCount)
               .Select(i =>
                {
                    var delta = DeltaHelper.GetDelta(
                        previousDeltaHash: GetHash(i - 1).AsBase32Address(),
                        timestamp: GetDateTimeForIndex(i));
                    return delta;
                })
               .ToList();

            Enumerable.Range(0, deltaCount).ToList().ForEach(i =>
            {
                ExpectTryGetDelta(GetHash(i).AsBase32Address(), deltas[i]);
            });
        }

        private void ExpectTryGetDelta(string hash, Delta delta)
        {
            _deltaCache.TryGetConfirmedDelta(hash, out Arg.Any<Delta>())
               .Returns(ci =>
                {
                    ci[1] = delta;
                    return true;
                });
        }
    }
}
