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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Extensions;
using Catalyst.Core.P2P.Models;
using Catalyst.Core.P2P.Repository;
using Catalyst.Core.Util;
using Catalyst.Node.POA.CE.Consensus.Deltas;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.POA.CE.UnitTests.Modules.Consensus.Deltas
{
    public class PoaDeltaProducersProviderTests
    {
        private readonly List<Peer> _peers;
        private readonly PoaDeltaProducersProvider _poaDeltaProducerProvider;
        private readonly IMultihashAlgorithm _hashAlgorithm;
        private readonly byte[] _previousDeltaHash;
        private readonly IMemoryCache _producersByPreviousDelta;
        private readonly string _previousDeltaHashString;

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

            var peerRepository = Substitute.For<IPeerRepository>();
            peerRepository.GetAll().Returns(_ => _peers);

            _previousDeltaHash = ByteUtil.GenerateRandomByteArray(32).ComputeMultihash(new BLAKE2B_256());
            _previousDeltaHashString = _previousDeltaHash.AsBase32Address();

            _hashAlgorithm = Substitute.For<IMultihashAlgorithm>();
            _hashAlgorithm.ComputeHash(Arg.Any<byte[]>()).Returns(ci => (byte[]) ci[0]);

            _producersByPreviousDelta = Substitute.For<IMemoryCache>();

            _poaDeltaProducerProvider = new PoaDeltaProducersProvider(peerRepository, PeerIdentifierHelper.GetPeerIdentifier("TEST"), _producersByPreviousDelta, _hashAlgorithm, logger);
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

            var peersAndNodeItselfCount = _peers.Count + 1;
            _hashAlgorithm.ReceivedWithAnyArgs(peersAndNodeItselfCount).ComputeHash(null);

            _producersByPreviousDelta.Received(1).TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashString)), out Arg.Any<object>());
            _producersByPreviousDelta.Received(1).CreateEntry(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashString)));

            producers.Count.Should().Be(expectedProducers.Count + 1, "producers are all the peers, and the node itself.");
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
            _producersByPreviousDelta.TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashString)), out Arg.Any<object>())
               .Returns(ci =>
                {
                    ci[1] = new List<IPeerIdentifier>();
                    return true;
                });

            var producers = _poaDeltaProducerProvider.GetDeltaProducersFromPreviousDelta(_previousDeltaHash);

            _hashAlgorithm.DidNotReceiveWithAnyArgs().ComputeHash(null);

            _producersByPreviousDelta.Received(1).TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHashString)), out Arg.Any<object>());
            _producersByPreviousDelta.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<string>());

            producers.Should().NotBeNull();
            producers.Count.Should().Be(0);
        }
    }
}
