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
using System.Runtime.CompilerServices;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Common.P2P.Discovery;
using Catalyst.Common.Util;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.Core.Lib.P2P.IO.Messaging.Correlation;
using Catalyst.Protocol.IPPN;
using DnsClient;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Nethereum.Hex.HexConvertors.Extensions;
using Nito.Comparers.Linq;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;
using Tmds.Linux;

namespace Catalyst.TestUtils
{
    public static class DiscoveryHelper
    {
        public static IHastingsOriginator MockOriginator(IPeerIdentifier peer = default,
            IList<INeighbour> neighbours = default,
            KeyValuePair<ICorrelationId, IPeerIdentifier> expectedPnr = default)
        {
            return new HastingsOriginator
            {
                Peer =
                    peer ?? PeerIdentifierHelper.GetPeerIdentifier(ByteUtil.GenerateRandomByteArray(32).ToString()),
                Neighbours = neighbours ?? MockNeighbours(),
                ExpectedPnr = expectedPnr
            }; 
        }
        
        public static IHastingsOriginator SubOriginator(IPeerIdentifier peer = default,
            IList<INeighbour> neighbours = default,
            KeyValuePair<ICorrelationId, IPeerIdentifier> expectedPnr = default)
        {
            var subbedOriginator = Substitute.For<IHastingsOriginator>();
            
            subbedOriginator.Neighbours.Count.Returns(Constants.AngryPirate);
            subbedOriginator.Peer.Returns(peer ?? Substitute.For<IPeerIdentifier>());
            subbedOriginator.Neighbours.Returns(neighbours ?? Substitute.For<IList<INeighbour>>());
            subbedOriginator.ExpectedPnr.Returns(expectedPnr);

            return subbedOriginator;
        }

        public static IHastingMemento MockSeedState(IPeerIdentifier ownNode, IPeerSettings peerSettings)
        {
            return MockMemento(ownNode, MockDnsClient(peerSettings)
               .GetSeedNodesFromDns(peerSettings.SeedServers)
               .ToNeighbours()
               .ToList()
            );
        }

        public static IHastingsOriginator SubSeedOriginator(IPeerIdentifier ownNode, IPeerSettings peerSettings)
        {
            var m = SubSeedState(ownNode, peerSettings);
            return SubOriginator(m.Peer, m.Neighbours);
        }

        public static IHastingMemento SubSeedState(IPeerIdentifier ownNode, IPeerSettings peerSettings)
        {
            return SubMemento(ownNode, MockDnsClient(peerSettings)
               .GetSeedNodesFromDns(peerSettings.SeedServers)
               .ToNeighbours()
               .ToList()
            );
        }
        
        public static KeyValuePair<ICorrelationId, IPeerIdentifier> MockPnr(IPeerIdentifier peerIdentifier = default, ICorrelationId correlationId = default)
        {
            return new KeyValuePair<ICorrelationId, IPeerIdentifier>(correlationId ?? CorrelationId.GenerateCorrelationId(),
                peerIdentifier ?? PeerIdentifierHelper.GetPeerIdentifier("sender")
            );
        }
        
        public static IList<INeighbour> MockNeighbours(int amount = 5, NeighbourState state = null, ICorrelationId correlationId = default)
        {
            var neighbourMock = new List<INeighbour>();

            Enumerable.Range(0, amount).ToList().ForEach(i =>
            {
                neighbourMock.Add(
                    new Neighbour(
                        PeerIdentifierHelper.GetPeerIdentifier(
                            StringHelper.RandomString()
                        ),
                        state ?? NeighbourState.NotContacted,
                        correlationId = correlationId == default ? null : CorrelationId.GenerateCorrelationId()
                    )
                );
            });
            
            return neighbourMock;
        }

        public static IDictionary<IPeerIdentifier, ICorrelationId> SubContactedNeighbours(int amount = 5)
        {
            return Enumerable.Range(0, amount)
               .Select(i => Substitute.For<IPeerIdentifier>())
               .ToDictionary(v => v, k => Substitute.For<ICorrelationId>());
        }
        
        public static IHastingMemento SubMemento(IPeerIdentifier identifier = default, IList<INeighbour> neighbours = default)
        {
            var subbedMemento = Substitute.For<IHastingMemento>();
            subbedMemento.Peer.Returns(identifier ?? Substitute.For<IPeerIdentifier>());
            subbedMemento.Neighbours.Returns(neighbours ?? MockNeighbours());

            return subbedMemento;
        }

        public static IHastingMemento MockMemento(IPeerIdentifier identifier = default, IList<INeighbour> neighbours = default)
        {
            var peerParam = identifier ?? PeerIdentifierHelper.GetPeerIdentifier(StringHelper.RandomString());
            var neighbourParam = neighbours ?? MockNeighbours();
            return new HastingMemento(peerParam, neighbourParam);
        }

        public static Stack<IHastingMemento> MockMementoHistory(Stack<IHastingMemento> state, int depth = 10)
        {
            while (true)
            {
                state.Push(
                    new HastingMemento(
                        state.Last()
                           .Neighbours
                           .RandomElement()
                           .PeerIdentifier, 
                        MockNeighbours()
                    )
                );

                if (state.Count != depth)
                {
                    return state;
                }
                
                depth = 10;
            }
        }

        public static IHastingCareTaker MockCareTaker(IEnumerable<IHastingMemento> history = default)
        {
            var careTaker = new HastingCareTaker();

            history?.ToList().ForEach(m =>
            {
                careTaker.Add(m);
            });

            return careTaker;
        }
        
        public static IDns MockDnsClient(IPeerSettings settings = default, ILookupClient lookupClient = default)
        {
            var peerSetting = settings ?? PeerSettingsHelper.TestPeerSettings();
             
            peerSetting.SeedServers.ToList().ForEach(domain =>
            {
                MockQueryResponse.CreateFakeLookupResult(domain, 
                    "0x" + PeerIdentifierHelper.GetPeerIdentifier(
                        StringHelper.RandomString(32)).ToString().ToHexUTF8(),
                    lookupClient ?? Substitute.For<ILookupClient>()
                );
            });
        
            return new Common.Network.DevDnsClient(settings);
        }

        public static IRepository<Peer> MockPeerRepository()
        {
            return new InMemoryRepository<Peer>();
        }
        
        public static IPeerClientMessageDto SubDto(Type discoveryMessage, ICorrelationId correlationId = default, IPeerIdentifier sender = default)
        {
            var dto = Substitute.For<IPeerClientMessageDto>();
            dto.Sender.Returns(sender ?? Substitute.For<IPeerIdentifier>());
            dto.CorrelationId.Returns(correlationId ?? Substitute.For<ICorrelationId>());
            dto.Message.Returns(Activator.CreateInstance(discoveryMessage));

            return dto;
        }
        
        public static ICancellationTokenProvider SubCancellationProvider(bool result = false)
        {
            var provider = Substitute.For<ICancellationTokenProvider>();
            provider.HasTokenCancelled().Returns(result);
            return provider;
        }

        public static IPeerMessageCorrelationManager MockCorrelationManager(IReputationManager reputationManager = default,
            IMemoryCache memoryCache = default,
            IChangeTokenProvider changeTokenProvider = default,
            ILogger logger = default)
        {
            return new PeerMessageCorrelationManager(
                reputationManager ?? Substitute.For<IReputationManager>(),
                memoryCache ?? Substitute.For<IMemoryCache>(),
                logger ?? Substitute.For<ILogger>(),
                changeTokenProvider ?? new TtlChangeTokenProvider(3));
        }
    }
}
