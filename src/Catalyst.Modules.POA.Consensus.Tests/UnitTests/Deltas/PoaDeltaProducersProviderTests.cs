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
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Modules.POA.Consensus.Deltas;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Ipfs;
using Ipfs.Registry;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using Xunit;
using Peer = Catalyst.Core.Lib.P2P.Models.Peer;

namespace Catalyst.Modules.POA.Consensus.Tests.UnitTests.Deltas
{
    public class PoaDeltaProducersProviderTests
    {
        private readonly List<Peer> _peers;
        private readonly PoaDeltaProducersProvider _poaDeltaProducerProvider;
        private readonly MultiHash _previousDeltaHash;
        private readonly IMemoryCache _producersByPreviousDelta;
        private readonly string _previousDeltaHashString;
        private readonly IHashProvider _hashProvider;

        public PoaDeltaProducersProviderTests()
        {
            var hashingAlgorithm = HashingAlgorithm.GetAlgorithmMetadata("blake2b-256");
            _hashProvider = new HashProvider(hashingAlgorithm);
            var rand = new Random();
            _peers = Enumerable.Range(0, 5)
               .Select(_ =>
                {
                    var peerIdentifier = PeerIdHelper.GetPeerId(rand.Next().ToString());

                    var peer = new Peer {PeerId = peerIdentifier};

                    return peer;
                }).ToList();

            var logger = Substitute.For<ILogger>();

            var peerRepository = Substitute.For<IPeerRepository>();
            peerRepository.GetAll().Returns(_ => _peers);

            _previousDeltaHash = _hashProvider.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(32));
            _previousDeltaHashString = _previousDeltaHash.ToBase32();

            _producersByPreviousDelta = Substitute.For<IMemoryCache>();

            _poaDeltaProducerProvider = new PoaDeltaProducersProvider(peerRepository,
                PeerIdHelper.GetPeerId("TEST").ToSubstitutedPeerSettings(),
                _producersByPreviousDelta,
                _hashProvider,
                logger);
        }

        [Fact]
        public void GetDeltaProducersFromPreviousDelta_when_not_cached_should_store_and_return_an_ordered_list()
        {
            _producersByPreviousDelta.TryGetValue(Arg.Any<string>(), out Arg.Any<object>()).Returns(false);

            var expectedProducers = _peers.Select(p =>
                {
                    var bytesToHash = p.PeerId.ToByteArray()
                       .Concat(_previousDeltaHash.ToArray()).ToArray();
                    var ranking = bytesToHash;
                    return new
                    {
                        PeerIdentifier = p.PeerId,
                        ranking
                    };
                })
               .OrderBy(h => h.ranking, ByteUtil.ByteListMinSizeComparer.Default)
               .Select(h => h.PeerIdentifier)
               .ToList();

            var producers = _poaDeltaProducerProvider.GetDeltaProducersFromPreviousDelta(_previousDeltaHash);

            var peersAndNodeItselfCount = _peers.Count + 1;

            _producersByPreviousDelta.Received(1).TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashString)),
                out Arg.Any<object>());
            _producersByPreviousDelta.Received(1)
               .CreateEntry(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashString)));

            producers.Count.Should()
               .Be(expectedProducers.Count + 1, "producers are all the peers, and the node itself.");
            producers.Should().OnlyHaveUniqueItems();

            for (var i = 0; i < expectedProducers.Count; i++)
            {
                producers[i].ToByteArray()
                   .Should().BeEquivalentTo(expectedProducers[i].ToByteArray());
            }
        }

        [Fact]
        public void GetDeltaProducersFromPreviousDelta_when_cached_should_not_recompute()
        {
            _producersByPreviousDelta.TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashString)),
                    out Arg.Any<object>())
               .Returns(ci =>
                {
                    ci[1] = new List<PeerId>();
                    return true;
                });

            var producers = _poaDeltaProducerProvider.GetDeltaProducersFromPreviousDelta(_previousDeltaHash);

            _producersByPreviousDelta.Received(1).TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashString)),
                out Arg.Any<object>());
            _producersByPreviousDelta.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<string>());

            producers.Should().NotBeNull();
            producers.Count.Should().Be(0);
        }
    }
}
