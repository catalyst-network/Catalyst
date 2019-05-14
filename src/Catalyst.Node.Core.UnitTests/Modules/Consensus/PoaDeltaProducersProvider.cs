using System;
using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Util;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Protocol.Delta;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.Modules.Consensus
{
    public class PoaDeltaProducersProviderTests
    {
        private readonly ITestOutputHelper _output;
        private readonly IPeerDiscovery _peerDiscovery;
        private readonly List<Peer> _peers;
        private readonly PoaDeltaProducersProvider _poaDeltaProducerProvider;
        private readonly Delta _delta;

        public PoaDeltaProducersProviderTests(ITestOutputHelper output)
        {
            _output = output;
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

            _peerDiscovery = Substitute.For<IPeerDiscovery>();
            _peerDiscovery.PeerRepository.GetAll().Returns(_ => _peers);

            var merkleRoot = new byte[32];
            rand.NextBytes(merkleRoot);
            _delta = new Delta() {MerkleRoot = merkleRoot.ToByteString()};

            _poaDeltaProducerProvider = new PoaDeltaProducersProvider(_peerDiscovery, logger);
        }

        [Fact]
        public void GetDeltaProducersFromPreviousDelta_should_return_an_ordered_list()
        {
            var expectedProducers = _peers.Select(p =>
                {
                    var ranking = _poaDeltaProducerProvider.TEMP_HASH_FUNCTION(
                        p.PeerIdentifier.PeerId.ToByteArray(), _delta.MerkleRoot.ToByteArray());
                    return new
                    {
                        PeerIdentifier = p.PeerIdentifier,
                        Ranking = ranking.ToArray()
                    };
                })
               .OrderBy(h => h.Ranking, ByteListComparer.Default)
               .Select(h => h.PeerIdentifier)
               .ToList();

            var producers = _poaDeltaProducerProvider.GetDeltaProducersFromPreviousDelta(_delta);

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
