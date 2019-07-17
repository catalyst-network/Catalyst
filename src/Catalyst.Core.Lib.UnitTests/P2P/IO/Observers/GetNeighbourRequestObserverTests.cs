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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using SharpRepository.Repository.Specifications;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P.IO.Observers
{
    public sealed class GetNeighbourRequestObserverTests : IDisposable
    {
        private readonly ILogger _subbedLogger;
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly IRepository<Peer, string> _subbedPeerRepository;

        public GetNeighbourRequestObserverTests()
        {
            _subbedLogger = Substitute.For<ILogger>();
            _subbedPeerRepository = Substitute.For<IRepository<Peer, string>>();
            _peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("testPeer");
        }
        
        private static void AddMockPeerToDbAndSetReturnExpectation(IReadOnlyList<Peer> peer,
            IRepository<Peer, string> store)
        {
            store.Add(peer);
            store.FindAll(Arg.Any<Specification<Peer>>()).Returns(peer);
        }

        [Fact]
        public async Task Can_Process_GetNeighbourRequest_Correctly()
        {
            // mock a random set of peers
            var randomPeers = new List<Peer>
            {
                new Peer {PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("peer1"), LastSeen = DateTime.Now},
                new Peer {PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("peer2"), LastSeen = DateTime.Now},
                new Peer {PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("peer3"), LastSeen = DateTime.Now},
                new Peer {PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("peer4"), LastSeen = DateTime.Now},
                new Peer {PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("peer5"), LastSeen = DateTime.Now},
                new Peer {PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("peer6")}
            };

            // add them to the mocked repository, and set return expectation
            AddMockPeerToDbAndSetReturnExpectation(randomPeers, _subbedPeerRepository);

            var neighbourRequestHandler = new GetNeighbourRequestObserver(_peerIdentifier,
                _subbedPeerRepository,
                _subbedLogger
            );
            
            var peerNeighbourRequestMessage = new PeerNeighborsRequest();
            
            var fakeContext = Substitute.For<IChannelHandlerContext>();
            var channeledAny = new ObserverDto(fakeContext, peerNeighbourRequestMessage.ToProtocolMessage(PeerIdHelper.GetPeerId(), CorrelationId.GenerateCorrelationId()));
            var observableStream = new[] {channeledAny}.ToObservable();
            
            neighbourRequestHandler.StartObserving(observableStream);
            
            var peerNeighborsResponseMessage = new PeerNeighborsResponse();
            
            for (var i = 0; i < 5; i++)
            {
                peerNeighborsResponseMessage.Peers.Add(PeerIdHelper.GetPeerId());
            }

            await observableStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            await fakeContext.Channel.ReceivedWithAnyArgs(1)
               .WriteAndFlushAsync(peerNeighborsResponseMessage.ToProtocolMessage(_peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId()));
        }

        public void Dispose()
        {
            _subbedPeerRepository?.Dispose();
        }
    }
}
