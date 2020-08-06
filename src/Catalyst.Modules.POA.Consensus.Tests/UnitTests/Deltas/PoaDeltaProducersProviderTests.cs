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
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Modules.POA.Consensus.Deltas;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Lib.P2P;
using Microsoft.Extensions.Caching.Memory;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Peer = Catalyst.Core.Lib.P2P.Models.Peer;
using MultiFormats;
using Catalyst.Core.Lib.Extensions;
using Nethermind.Core;

namespace Catalyst.Modules.POA.Consensus.Tests.UnitTests.Deltas
{
    public class PoaDeltaProducersProviderTests
    {
        private Peer _selfAsPeer;
        private List<Peer> _peers;
        private PoaDeltaProducersProvider _poaDeltaProducerProvider;
        private Cid _previousDeltaHash;
        private IMemoryCache _producersByPreviousDelta;
        private IHashProvider _hashProvider;

        [SetUp]
        public void Init()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));

            var peerSettings = MultiAddressHelper.GetAddress("TEST").ToSubstitutedPeerSettings();
            _selfAsPeer = new Peer { Address = peerSettings.Address };
            var rand = new Random();
            _peers = Enumerable.Range(0, 5)
               .Select(_ =>
                {
                    var peerIdentifier = MultiAddressHelper.GetAddress(rand.Next().ToString());
                    var peer = new Peer { Address = peerIdentifier, LastSeen = DateTime.UtcNow };
                    return peer;
                }).ToList();

            var logger = Substitute.For<ILogger>();

            var peerRepository = Substitute.For<IPeerRepository>();
            peerRepository.GetActivePoaPeers().Returns(_ => _peers);

            _previousDeltaHash =
                _hashProvider.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(32)).ToCid();

            _producersByPreviousDelta = Substitute.For<IMemoryCache>();

            _poaDeltaProducerProvider = new PoaDeltaProducersProvider(peerRepository,
                peerSettings,
                _producersByPreviousDelta,
                _hashProvider,
                logger);
        }

        [Test]
        public void GetDeltaProducersFromPreviousDelta_when_not_cached_should_store_and_return_an_ordered_list()
        {
            _producersByPreviousDelta.TryGetValue(Arg.Any<string>(), out Arg.Any<object>()).Returns(false);

            var peers = _peers.Select(x=>x.Address.GetKvmAddress());

            var expectedProducers = peers.Select(p =>
                {
                    var ranking = _hashProvider.ComputeMultiHash(p.Bytes, _previousDeltaHash.ToArray()).ToArray();
                    return new
                    {
                        KvmAddress = p,
                        ranking
                    };
                })
               .OrderBy(h => h.ranking, ByteUtil.ByteListMinSizeComparer.Default)
               .Select(h => h.KvmAddress)
               .ToList();

            var producers = _poaDeltaProducerProvider.GetDeltaProducersFromPreviousDelta(_previousDeltaHash);

            _producersByPreviousDelta.Received(1).TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHash)),
                out Arg.Any<object>());
            _producersByPreviousDelta.Received(1)
               .CreateEntry(Arg.Is<string>(s => s.EndsWith(_previousDeltaHash)));

            producers.Should().OnlyHaveUniqueItems();

            for (var i = 0; i < expectedProducers.Count; i++)
            {
                producers[i].Should().BeEquivalentTo(expectedProducers[i]);
            }
        }

        [Test]
        public void GetDeltaProducersFromPreviousDelta_when_cached_should_not_recompute()
        {
            _producersByPreviousDelta.TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHash)),
                    out Arg.Any<object>())
               .Returns(ci =>
                {
                    ci[1] = new List<Address>();
                    return true;
                });

            var producers = _poaDeltaProducerProvider.GetDeltaProducersFromPreviousDelta(_previousDeltaHash);

            _producersByPreviousDelta.Received(1).TryGetValue(Arg.Is<string>(s => s.EndsWith(_previousDeltaHash)),
                out Arg.Any<object>());
            _producersByPreviousDelta.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<string>());

            producers.Should().NotBeNull();
            producers.Count.Should().Be(0);
        }
    }
}
