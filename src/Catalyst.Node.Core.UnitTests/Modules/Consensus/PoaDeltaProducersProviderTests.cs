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
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Util;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Protocol.Delta;
using FluentAssertions;
using Google.Protobuf;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;
using Xunit.Abstractions;
using Peer = Catalyst.Common.P2P.Peer;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus
{
    public class PoaDeltaProducersProviderTests
    {
        private readonly List<Peer> _peers;
        private readonly PoaDeltaProducersProvider _poaDeltaProducerProvider;
        private readonly IMultihashAlgorithm _hashAlgorithm;
        private readonly byte[] _previousDeltaHash;

        public PoaDeltaProducersProviderTests(ITestOutputHelper output)
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
            
            _hashAlgorithm = Common.Config.Constants.HashAlgorithm;

            _poaDeltaProducerProvider = new PoaDeltaProducersProvider(peerRepository, logger);
        }

        [Fact]
        public void GetDeltaProducersFromPreviousDelta_should_return_an_ordered_list()
        {
            var expectedProducers = _peers.Select(p =>
                {
                    var bytesToHash = p.PeerIdentifier.PeerId.ToByteArray()
                       .Concat(_previousDeltaHash).ToArray();
                    var ranking = _hashAlgorithm.ComputeHash(bytesToHash);
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

            producers.Count.Should().Be(expectedProducers.Count);
            producers.Should().OnlyHaveUniqueItems();

            for (var i = 0; i < expectedProducers.Count; i++)
            {
                producers[i].PeerId.ToByteArray()
                   .Should().BeEquivalentTo(expectedProducers[i].PeerId.ToByteArray());
            }
        }
    }
}
