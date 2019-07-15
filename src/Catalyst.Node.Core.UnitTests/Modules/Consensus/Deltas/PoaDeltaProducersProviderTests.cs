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
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Util;
using Catalyst.Node.Core.Modules.Consensus.Deltas;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Multiformats.Hash.Algorithms;
using Nethereum.Hex.HexConvertors.Extensions;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;
using Peer = Catalyst.Common.P2P.Peer;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus.Deltas
{
    public class PoaDeltaProducersProviderTests
    {
        private readonly List<Peer> _peers;
        private readonly PoaDeltaProducersProvider _poaDeltaProducerProvider;
        private readonly IMultihashAlgorithm _hashAlgorithm;
        private readonly byte[] _previousDeltaHash;
        private readonly IMemoryCache _producersByPreviousDelta;
        private readonly string _previousDeltaHashHex;

        public PoaDeltaProducersProviderTests()
        {
            var rand = new Random();
            _peers = Enumerable.Range(0, 5)
               .Select(_ =>
                {
                    var peerIdentifier = PeerIdentifierHelper
                       .GetPeerIdentifier(rand.Next().ToString());

                    var peer = new Peer {PeerIdentifier = peerIdentifier};

                    return peer;
                }).ToList();

            var logger = Substitute.For<ILogger>();

            var peerRepository = Substitute.For<IRepository<Peer>>();
            peerRepository.GetAll().Returns(_ => _peers);

            _previousDeltaHash = new byte[32];
            rand.NextBytes(_previousDeltaHash);
            _previousDeltaHashHex = _previousDeltaHash.ToHex();

            _hashAlgorithm = Substitute.For<IMultihashAlgorithm>();
            _hashAlgorithm.ComputeHash(Arg.Any<byte[]>()).Returns(ci => (byte[]) ci[0]);

            _producersByPreviousDelta = Substitute.For<IMemoryCache>();

            _poaDeltaProducerProvider = new PoaDeltaProducersProvider(peerRepository, _producersByPreviousDelta, _hashAlgorithm, logger);
        }

        [Fact]
        public void GetDeltaProducersFromPreviousDelta_when_not_cached_should_store_and_return_an_ordered_list()
        {
            _producersByPreviousDelta.TryGetValue(Arg.Any<string>(), out Arg.Any<object>()).Returns(false);

            var expectedProducers = _peers.Select(p =>
                {
                    var bytesToHash = p.PeerIdentifier.PeerId.ToByteArray()
                       .Concat(_previousDeltaHash).ToArray();
                    var ranking = bytesToHash;
                    return new
                    {
                        p.PeerIdentifier,
                        ranking
                    };
                })
               .OrderBy(h => h.ranking, ByteUtil.ByteListMinSizeComparer.Default)
               .Select(h => h.PeerIdentifier)
               .ToList();

            var producers = _poaDeltaProducerProvider.GetDeltaProducersFromPreviousDelta(_previousDeltaHash);

            _hashAlgorithm.ReceivedWithAnyArgs(_peers.Count).ComputeHash(null);

            _producersByPreviousDelta.Received(1).TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashHex)), out Arg.Any<object>());
            _producersByPreviousDelta.Received(1).CreateEntry(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashHex)));

            producers.Count.Should().Be(expectedProducers.Count);
            producers.Should().OnlyHaveUniqueItems();

            for (var i = 0; i < expectedProducers.Count; i++)
            {
                producers[i].PeerId.ToByteArray()
                   .Should().BeEquivalentTo(expectedProducers[i].PeerId.ToByteArray());
            }
        }

        [Fact]
        public void GetDeltaProducersFromPreviousDelta_when_cached_should_not_recompute()
        {
            _producersByPreviousDelta.TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashHex)), out Arg.Any<object>())
               .Returns(ci =>
                {
                    ci[1] = new List<IPeerIdentifier>();
                    return true;
                });

            var producers = _poaDeltaProducerProvider.GetDeltaProducersFromPreviousDelta(_previousDeltaHash);

            _hashAlgorithm.DidNotReceiveWithAnyArgs().ComputeHash(null);

            _producersByPreviousDelta.Received(1).TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashHex)), out Arg.Any<object>());
            _producersByPreviousDelta.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<string>());

            producers.Should().NotBeNull();
            producers.Count.Should().Be(0);
        }
    }
}
