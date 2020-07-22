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
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using MultiFormats;
using Catalyst.Abstractions.P2P;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Observers
{
    public sealed class GetNeighbourRequestObserverTests : IDisposable
    {
        private readonly TestScheduler _testScheduler;
        private readonly ILogger _subbedLogger;
        private readonly MultiAddress _peerId;
        private readonly IPeerRepository _subbedPeerRepository;
        private readonly IPeerClient _peerClient;

        public GetNeighbourRequestObserverTests()
        {
            _testScheduler = new TestScheduler();
            _subbedLogger = Substitute.For<ILogger>();
            _subbedPeerRepository = Substitute.For<IPeerRepository>();
            _peerId = MultiAddressHelper.GetAddress("testPeer");
            _peerClient = Substitute.For<IPeerClient>();
        }

        private static void AddMockPeerToDbAndSetReturnExpectation(IReadOnlyList<Peer> peer,
            IPeerRepository repository)
        {
            repository.Add(peer);
            repository.GetActivePeers(Arg.Any<int>()).Returns(peer);
        }

        [Test]
        public async Task Can_Process_GetNeighbourRequest_Correctly()
        {
            // mock a random set of peers
            var randomPeers = new List<Peer>
            {
                new Peer {Address = MultiAddressHelper.GetAddress("peer1"), LastSeen = DateTime.Now},
                new Peer {Address = MultiAddressHelper.GetAddress("peer2"), LastSeen = DateTime.Now},
                new Peer {Address = MultiAddressHelper.GetAddress("peer3"), LastSeen = DateTime.Now},
                new Peer {Address = MultiAddressHelper.GetAddress("peer4"), LastSeen = DateTime.Now},
                new Peer {Address = MultiAddressHelper.GetAddress("peer5"), LastSeen = DateTime.Now},
                new Peer {Address = MultiAddressHelper.GetAddress("peer6")}
            };

            // add them to the mocked repository, and set return expectation
            AddMockPeerToDbAndSetReturnExpectation(randomPeers, _subbedPeerRepository);

            var peerSettings = _peerId.ToSubstitutedPeerSettings();
            var neighbourRequestHandler = new GetNeighbourRequestObserver(peerSettings,
                _subbedPeerRepository,
                _peerClient,
                _subbedLogger
            );

            var peerNeighbourRequestMessage = new PeerNeighborsRequest();

            var channeledAny = peerNeighbourRequestMessage.ToProtocolMessage(MultiAddressHelper.GetAddress(),
                    CorrelationId.GenerateCorrelationId());
            var observableStream = new[] {channeledAny}.ToObservable(_testScheduler);

            neighbourRequestHandler.StartObserving(observableStream);

            var peerNeighborsResponseMessage = new PeerNeighborsResponse();

            for (var i = 0; i < 5; i++)
            {
                peerNeighborsResponseMessage.Peers.Add(MultiAddressHelper.GetAddress().ToString());
            }

            _testScheduler.Start();

            await _peerClient.ReceivedWithAnyArgs(1).SendMessageAsync(peerNeighborsResponseMessage.ToProtocolMessage(_peerId, CorrelationId.GenerateCorrelationId()),
            _peerId).ConfigureAwait(false);
        }

        public void Dispose() { _subbedPeerRepository?.Dispose(); }
    }
}
