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
using System.Threading;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using FluentAssertions;
using MultiFormats.Registry;
using NSubstitute;
using PeerTalk;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Ledger.Tests.UnitTests
{
    public class LedgerSynchroniserTests
    {
        private readonly ITestOutputHelper _output;
        private readonly IDeltaCache _deltaCache;
        private readonly LedgerSynchroniser _synchroniser;
        private readonly CancellationToken _cancellationToken;
        private readonly HashProvider _hashProvider;

        public LedgerSynchroniserTests(ITestOutputHelper output)
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));

            _output = output;
            _deltaCache = Substitute.For<IDeltaCache>();
            var logger = Substitute.For<ILogger>();

            _cancellationToken = new CancellationToken();

            _synchroniser = new LedgerSynchroniser(_deltaCache, logger);
        }

        private Dictionary<Cid, Delta> BuildChainedDeltas(int chainSize)
        {
            var chainedDeltas = Enumerable.Range(0, chainSize + 1).ToDictionary(
                i => CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash(i.ToString())),
                i =>
                {
                    var previousHash = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash((i - 1).ToString()));
                    var delta = DeltaHelper.GetDelta(_hashProvider, previousHash);
                    return delta;
                });

            _output.WriteLine("chain is:");
            _output.WriteLine(string.Join(Environment.NewLine,
                chainedDeltas.Select((c, i) =>
                    $"{i}: current {c.Key} | previous {CidHelper.Cast(c.Value.PreviousDeltaDfsHash.ToByteArray())}")));
            return chainedDeltas;
        }

        private void SetCacheExpectations(Dictionary<Cid, Delta> deltasByHash)
        {
            foreach (var delta in deltasByHash)
            {
                _deltaCache.TryGetOrAddConfirmedDelta(delta.Key, out Arg.Any<Delta>())
                   .Returns(ci =>
                    {
                        ci[1] = delta.Value;
                        return true;
                    });
            }
        }

        [Fact]
        public void CacheDeltasBetween_Should_Stop_When_One_Of_Deltas_Is_Missing()
        {
            var chainSize = 5;
            var chain = BuildChainedDeltas(chainSize);
            SetCacheExpectations(chain);

            var hashes = chain.Keys.ToArray();
            var brokenChainIndex = 2;
            _deltaCache.TryGetOrAddConfirmedDelta(hashes[brokenChainIndex], out Arg.Any<Delta>())
               .Returns(false);
            _output.WriteLine($"chain is broken for {hashes[brokenChainIndex]}, it cannot be found on Dfs.");

            var cachedHashes = _synchroniser.CacheDeltasBetween(hashes.First(),
                hashes.Last(), _cancellationToken).ToList();

            OutputCachedHashes(cachedHashes);

            cachedHashes.Count.Should().Be(chainSize - brokenChainIndex);
            hashes.TakeLast(chainSize - brokenChainIndex + 1).ToList().ForEach(h =>
            {
                _deltaCache.Received(1).TryGetOrAddConfirmedDelta(h,
                    out Arg.Any<Delta>(), _cancellationToken);
            });
        }

        private void OutputCachedHashes(List<Cid> cachedHashes)
        {
            _output.WriteLine("cached hashes between: ");
            _output.WriteLine(string.Join(", ", cachedHashes));
        }

        [Fact]
        public void CacheDeltasBetween_Should_Complete_When_LatestKnownDelta_Is_Found()
        {
            var chainSize = 7;
            var chain = BuildChainedDeltas(chainSize);
            SetCacheExpectations(chain);

            var hashes = chain.Keys.ToArray();

            var latestHashIndex = 3;
            _output.WriteLine($"Caching deltas between {hashes[latestHashIndex]} and {hashes.Last()}");
            var cachedHashes = _synchroniser.CacheDeltasBetween(hashes[latestHashIndex],
                hashes.Last(), _cancellationToken).ToList();

            var expectedResultLength = chainSize - latestHashIndex + 1;
            cachedHashes.Count.Should().Be(expectedResultLength);

            OutputCachedHashes(cachedHashes);

            cachedHashes.Should().BeEquivalentTo(hashes.TakeLast(expectedResultLength));

            hashes.TakeLast(expectedResultLength - 1).Reverse().ToList().ForEach(h =>
            {
                _deltaCache.Received(1).TryGetOrAddConfirmedDelta(h,
                    out Arg.Any<Delta>(), _cancellationToken);
            });
        }
    }
}
