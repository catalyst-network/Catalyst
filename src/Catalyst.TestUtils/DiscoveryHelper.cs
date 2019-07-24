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
            IList<IPeerIdentifier> currentPeersNeighbours = default,
            KeyValuePair<ICorrelationId, IPeerIdentifier> expectedPnr = default,
            IDictionary<IPeerIdentifier, ICorrelationId> unResponsivePeers = null)
        {
            return new HastingsOriginator
            {
                Peer =
                    peer ?? PeerIdentifierHelper.GetPeerIdentifier(ByteUtil.GenerateRandomByteArray(32).ToString()),
                CurrentPeersNeighbours = currentPeersNeighbours ?? MockNeighbours(),
                ExpectedPnr = expectedPnr,
                UnResponsivePeers = unResponsivePeers ?? MockContactedNeighboursValuePairs()
            }; 
        }
        
        public static IHastingsOriginator SubOriginator(IPeerIdentifier peer = default,
            IList<IPeerIdentifier> currentPeersNeighbours = default,
            KeyValuePair<ICorrelationId, IPeerIdentifier> expectedPnr = default,
            IDictionary<IPeerIdentifier, ICorrelationId> unresponsivePeers = default)
        {
            var subbedOriginator = Substitute.For<IHastingsOriginator>();
            
            subbedOriginator.UnResponsivePeers.Count.Returns(5);
            subbedOriginator.Peer.Returns(peer ?? Substitute.For<IPeerIdentifier>());
            subbedOriginator.CurrentPeersNeighbours.Returns(currentPeersNeighbours ?? Substitute.For<IList<IPeerIdentifier>>());
            subbedOriginator.ExpectedPnr.Returns(expectedPnr);
            subbedOriginator.UnResponsivePeers.Returns(unresponsivePeers ?? Substitute.For<IDictionary<IPeerIdentifier, ICorrelationId>>());

            return subbedOriginator;
        }

        public static IHastingMemento MockSeedState(IPeerIdentifier ownNode, IPeerSettings peerSettings)
        {
            return MockMemento(ownNode, MockDnsClient(peerSettings)
               .GetSeedNodesFromDns(peerSettings.SeedServers).ToList());
        }

        public static IHastingsOriginator SubSeedOriginator(IPeerIdentifier ownNode, IPeerSettings peerSettings)
        {
            var m = SubSeedState(ownNode, peerSettings);
            return SubOriginator(m.Peer, m.Neighbours);
        }

        public static IHastingMemento SubSeedState(IPeerIdentifier ownNode, IPeerSettings peerSettings)
        {
            return SubMemento(ownNode, MockDnsClient(peerSettings)
               .GetSeedNodesFromDns(peerSettings.SeedServers).ToList());
        }
        
        public static KeyValuePair<ICorrelationId, IPeerIdentifier> MockPnr(IPeerIdentifier peerIdentifier = default, ICorrelationId correlationId = default)
        {
            return new KeyValuePair<ICorrelationId, IPeerIdentifier>(correlationId ?? CorrelationId.GenerateCorrelationId(), peerIdentifier ?? PeerIdentifierHelper.GetPeerIdentifier("sender"));
        }
        
        public static IList<IPeerIdentifier> SubNeighbours(int amount = 5)
        {
            return Enumerable.Range(0, amount).Select(i => Substitute.For<IPeerIdentifier>()).ToList();
        }
        
        public static IList<IPeerIdentifier> MockNeighbours(int amount = 5)
        {
            return Enumerable.Range(0, amount).Select(i =>
                PeerIdentifierHelper.GetPeerIdentifier(Helper.RandomString())).ToList();
        }

        public static IDictionary<IPeerIdentifier, ICorrelationId> MockUnResponsiveNeighbours(IEnumerable<IPeerIdentifier> neighboursParam = default)
        {
            IList<IPeerIdentifier> neighbours = null;
            if (neighboursParam == null || neighboursParam.Equals(default))
            {
                neighbours = MockNeighbours();
            }
            
            var mockUnResponsiveNeighboursList = new Dictionary<IPeerIdentifier, ICorrelationId>();
            
            neighbours?.ToList().ForEach(i =>
            {
                mockUnResponsiveNeighboursList.Add(i, null);
            });

            return mockUnResponsiveNeighboursList;
        }

        public static IDictionary<IPeerIdentifier, ICorrelationId> SubContactedNeighbours(int amount = 5)
        {
            return Enumerable.Range(0, amount).Select(i => Substitute.For<IPeerIdentifier>()).ToDictionary(v => v, k => Substitute.For<ICorrelationId>());
        }

        public static IDictionary<IPeerIdentifier, ICorrelationId> MockContactedNeighboursValuePairs(IEnumerable<IPeerIdentifier> neighbours = default)
        {
            if (neighbours == null || neighbours.Equals(default))
            {
                neighbours = MockNeighbours();
            }
            
            var mockContactedNeighboursList = new Dictionary<IPeerIdentifier, ICorrelationId>();
            
            neighbours?.ToList().ForEach(i =>
            {
                mockContactedNeighboursList.Add(i, CorrelationId.GenerateCorrelationId());
            });

            return mockContactedNeighboursList;
        }
        
        public static IHastingMemento SubMemento(IPeerIdentifier identifier = default, IEnumerable<IPeerIdentifier> neighbours = default)
        {
            var subbedMemento = Substitute.For<IHastingMemento>();
            subbedMemento.Peer.Returns(identifier ?? Substitute.For<IPeerIdentifier>());
            subbedMemento.Neighbours.Returns(neighbours ?? SubNeighbours());

            return subbedMemento;
        }

        public static IHastingMemento MockMemento(IPeerIdentifier identifier = default, IEnumerable<IPeerIdentifier> neighbours = default)
        {
            var peerParam = identifier ?? PeerIdentifierHelper.GetPeerIdentifier(Helper.RandomString());
            var neighbourParam = neighbours ?? MockNeighbours();
            return new HastingMemento(peerParam, neighbourParam);
        }

        public static Stack<IHastingMemento> MockMementoHistory(Stack<IHastingMemento> state, int depth = 10)
        {
            while (true)
            {
                state.Push(new HastingMemento(state.Last().Neighbours.RandomElement(), MockNeighbours()));

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
                        Helper.RandomString(32)).ToString().ToHexUTF8(),
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
