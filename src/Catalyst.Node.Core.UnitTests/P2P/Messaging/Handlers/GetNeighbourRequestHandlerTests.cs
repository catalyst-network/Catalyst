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
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.P2P.Messaging.Handlers;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.P2P.Messaging.Handlers
{
    public sealed class GetNeighbourRequestHandlerTests : ConfigFileBasedTest
    {
        private readonly ILogger _subbedLogger;
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly IRepository<Peer> _subbedPeerRepository;

        public GetNeighbourRequestHandlerTests(ITestOutputHelper output) : base(output)
        {
            _subbedLogger = Substitute.For<ILogger>();
            _subbedPeerRepository = Substitute.For<IRepository<Peer>>();
            _peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("testPeer");
        }
        
        private static void AddMockPeerToDbAndSetReturnExpectation(IReadOnlyList<Peer> peer,
            IRepository<Peer, int> store)
        {
            store.Add(peer);
            
            // store.FindAll(new Specification<Peer>(p => p.IsAwolPeer == false))
            
            store.GetAll().Returns(peer);
        }
        
        [Fact]
        public void CanInitGetNeighbourRequestHandlerCorrectly()
        {   
            var neighbourRequestHandler = new GetNeighbourRequestHandler(_peerIdentifier,
                _subbedPeerRepository,
                _subbedLogger
            );

            neighbourRequestHandler.Should().NotBeNull();
        }
        
        [Fact]
        public void CanResolveGetNeighbourRequestHandlerFromContainer()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Test)))
               .Build();
            
            ConfigureContainerBuilder(config, true, true);

            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var p2PMessageHandlers = container.Resolve<IEnumerable<IP2PMessageHandler>>();
                IEnumerable<IP2PMessageHandler> getNeighbourResponseHandler = p2PMessageHandlers.OfType<GetNeighbourRequestHandler>();
                getNeighbourResponseHandler.First().Should().BeOfType(typeof(GetNeighbourRequestHandler));
            }
        }

        [Fact]
        public async Task CanHandlerGetNeighbourRequestHandlerCorrectly()
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

            var neighbourRequestHandler = new GetNeighbourRequestHandler(_peerIdentifier,
                _subbedPeerRepository,
                _subbedLogger
            );
            
            var peerNeighbourRequestMessage = new PeerNeighborsRequest();
            
            var fakeContext = Substitute.For<IChannelHandlerContext>();
            var channeledAny = new ProtocolMessageDto(fakeContext, peerNeighbourRequestMessage.ToAnySigned(PeerIdHelper.GetPeerId(), Guid.NewGuid()));
            var observableStream = new[] {channeledAny}.ToObservable();
            
            neighbourRequestHandler.StartObserving(observableStream);
            
            var peerNeighborsResponseMessage = new PeerNeighborsResponse();
            
            for (var i = 0; i < 5; i++)
            {
                peerNeighborsResponseMessage.Peers.Add(PeerIdHelper.GetPeerId());
            }

            await observableStream.WaitForEndOfDelayedStreamOnTaskPoolScheduler();

            await fakeContext.Channel.ReceivedWithAnyArgs(1)
               .WriteAndFlushAsync(peerNeighborsResponseMessage.ToAnySigned(_peerIdentifier.PeerId, Guid.NewGuid()));
        }
    }
}
