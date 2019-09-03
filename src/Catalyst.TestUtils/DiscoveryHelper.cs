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
using System.Reactive.Concurrency;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Network;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Abstractions.Types;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Network;
using Catalyst.Core.P2P.Discovery;
using Catalyst.Core.P2P.Discovery.Hastings;
using Catalyst.Core.P2P.IO.Messaging.Correlation;
using Catalyst.Core.P2P.Models;
using Catalyst.Core.P2P.ReputationSystem;
using Catalyst.Core.Util;
using DnsClient;
using Microsoft.Extensions.Caching.Memory;
using Nethereum.Hex.HexConvertors.Extensions;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;

namespace Catalyst.TestUtils
{
    public static class DiscoveryHelper
    {
        public static IHastingsOriginator MockOriginator(IPeerIdentifier peer = default,
            INeighbours neighbours = default)
        {
            var memento = new HastingsMemento(peer, neighbours);
            return new HastingsOriginator(memento);
        }

        public static IHastingsOriginator SubOriginator(IPeerIdentifier peer = default,
            INeighbours neighbours = default,
            ICorrelationId expectedPnr = default)
        {
            var subbedOriginator = Substitute.For<IHastingsOriginator>();

            subbedOriginator.Neighbours.Count.Returns(5);
            subbedOriginator.Peer.Returns(peer ?? Substitute.For<IPeerIdentifier>());
            subbedOriginator.Neighbours.Returns(neighbours ?? Substitute.For<INeighbours>());
            subbedOriginator.PnrCorrelationId.Returns(expectedPnr ?? CorrelationId.GenerateCorrelationId());

            return subbedOriginator;
        }

        public static IHastingsMemento MockSeedState(IPeerIdentifier ownNode, IPeerSettings peerSettings)
        {
            return MockMemento(ownNode, MockDnsClient(peerSettings)
               .GetSeedNodesFromDns(peerSettings.SeedServers)
               .ToNeighbours()
            );
        }

        public static IHastingsOriginator SubSeedOriginator(IPeerIdentifier ownNode, IPeerSettings peerSettings)
        {
            var m = SubSeedState(ownNode, peerSettings);
            return SubOriginator(m.Peer, m.Neighbours);
        }

        public static IHastingsMemento SubSeedState(IPeerIdentifier ownNode, IPeerSettings peerSettings)
        {
            var neighbours = MockDnsClient(peerSettings)
               .GetSeedNodesFromDns(peerSettings.SeedServers)
               .ToNeighbours();

            return SubMemento(ownNode, neighbours);
        }

        public static INeighbours MockNeighbours(int amount = 5,
            NeighbourStateTypes stateTypes = null,
            ICorrelationId correlationId = default)
        {
            var neighbours = Enumerable.Range(0, amount).Select(i =>
                new Neighbour(
                    PeerIdentifierHelper.GetPeerIdentifier(
                        StringHelper.RandomString()
                    ),
                    stateTypes ?? NeighbourStateTypes.NotContacted,
                    correlationId ?? CorrelationId.GenerateCorrelationId()));

            return new Neighbours(neighbours);
        }

        public static IDictionary<IPeerIdentifier, ICorrelationId> SubContactedNeighbours(int amount = 5)
        {
            return Enumerable.Range(0, amount)
               .Select(i => Substitute.For<IPeerIdentifier>())
               .ToDictionary(v => v, k => Substitute.For<ICorrelationId>());
        }

        public static IHastingsMemento SubMemento(IPeerIdentifier identifier = default,
            INeighbours neighbours = default)
        {
            var subbedMemento = Substitute.For<IHastingsMemento>();
            subbedMemento.Peer.Returns(identifier ?? Substitute.For<IPeerIdentifier>());
            subbedMemento.Neighbours.Returns(neighbours ?? MockNeighbours());

            return subbedMemento;
        }

        public static IHastingsMemento MockMemento(IPeerIdentifier identifier = default,
            INeighbours neighbours = default)
        {
            var peerParam = identifier ?? PeerIdentifierHelper.GetPeerIdentifier(StringHelper.RandomString());
            var neighbourParam = neighbours ?? MockNeighbours();
            return new HastingsMemento(peerParam, neighbourParam);
        }

        public static Stack<IHastingsMemento> MockMementoHistory(Stack<IHastingsMemento> state, int depth = 10)
        {
            while (true)
            {
                state.Push(
                    new HastingsMemento(
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

        public static IHastingsCareTaker MockCareTaker(IEnumerable<IHastingsMemento> history = default)
        {
            var careTaker = new HastingsCareTaker();

            history?.ToList().ForEach(m => { careTaker.Add(m); });

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

            return new DevDnsClient(settings);
        }

        public static IRepository<Peer> MockPeerRepository() { return new InMemoryRepository<Peer>(); }

        public static IPeerClientMessageDto SubDto(Type discoveryMessage,
            ICorrelationId correlationId = default,
            IPeerIdentifier sender = default)
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

        public static IPeerMessageCorrelationManager MockCorrelationManager(IScheduler scheduler,
            IReputationManager reputationManager = default,
            IMemoryCache memoryCache = default,
            IChangeTokenProvider changeTokenProvider = default,
            ILogger logger = default)
        {
            return new PeerMessageCorrelationManager(
                reputationManager ?? Substitute.For<IReputationManager>(),
                memoryCache ?? Substitute.For<IMemoryCache>(),
                logger ?? Substitute.For<ILogger>(),
                changeTokenProvider ?? new TtlChangeTokenProvider(3),
                scheduler);
        }
    }
}
