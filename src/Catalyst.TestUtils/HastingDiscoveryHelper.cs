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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO;
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
using NSubstitute;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.TestUtils
{
    public static class HastingDiscoveryHelper
    {
        public class HastingDiscoveryTestBuilder
        {
            private readonly List<Action<HastingDiscoveryTest>> _builderActions;

            public static HastingDiscoveryTestBuilder CreateHastingDiscoveryTestBuilder()
            {
                return new HastingDiscoveryTestBuilder();
            }
        
            public HastingDiscoveryTestBuilder()
            {
                _builderActions = new List<Action<HastingDiscoveryTest>>();
            }
        }

        public sealed class HastingDiscoveryTest : HastingsDiscovery
        {
            internal HastingDiscoveryTest(ILogger logger = default,
                IRepository<Peer> peerRepository = default,
                IDns dns = default,
                IPeerSettings peerSettings = default,
                IPeerClient peerClient = default,
                IDtoFactory dtoFactory = default,
                IPeerMessageCorrelationManager peerMessageCorrelationManager = default,
                ICancellationTokenProvider cancellationTokenProvider = default,
                IEnumerable<IPeerClientObservable> peerClientObservables = default,
                bool autoStart = true,
                int peerDiscoveryBurnIn = 10,
                IHastingsOriginator state = default,
                IHastingCareTaker hastingCareTaker = default,
                IHastingsOriginator stateCandidate = default) 
                : base(logger ?? Substitute.For<ILogger>(),
                    peerRepository ?? Substitute.For<IRepository<Peer>>(),
                    dns ?? MockDnsClient(peerSettings),
                    peerSettings ?? PeerSettingsHelper.TestPeerSettings(),
                    peerClient ?? Substitute.For<IPeerClient>(),
                    dtoFactory ?? Substitute.For<IDtoFactory>(),
                    peerMessageCorrelationManager ?? MockCorrelationManager(),
                    cancellationTokenProvider ?? new CancellationTokenProvider(),
                    peerClientObservables,
                    autoStart,
                    peerDiscoveryBurnIn,
                    state ?? Substitute.For<IHastingsOriginator>(),
                    hastingCareTaker ?? Substitute.For<IHastingCareTaker>(),
                    stateCandidate ?? Substitute.For<IHastingsOriginator>()) { }

            public new void WalkForward() { base.WalkForward(); }

            public new void WalkBack() { base.WalkBack(); }

            public new bool HasValidCandidate() { return base.HasValidCandidate(); }
        }

        public static HastingDiscoveryTest GetTestInstanceOfDiscovery(ILogger logger,
            IRepository<Peer> peerRepository,
            IDns dns,
            IPeerSettings peerSettings,
            IPeerClient peerClient,
            IDtoFactory dtoFactory,
            IPeerMessageCorrelationManager peerMessageCorrelationManager,
            ICancellationTokenProvider cancellationTokenProvider,
            IEnumerable<IPeerClientObservable> peerClientObservables,
            bool autoStart = true,
            int peerDiscoveryBurnIn = 10,
            IHastingsOriginator state = default,
            IHastingCareTaker hastingCareTaker = default,
            IHastingsOriginator stateCandidate = default)
        {
            return new HastingDiscoveryTest(logger,
                peerRepository,
                dns,
                peerSettings,
                peerClient,
                dtoFactory,
                peerMessageCorrelationManager,
                cancellationTokenProvider,
                peerClientObservables,
                autoStart,
                peerDiscoveryBurnIn,
                state,
                hastingCareTaker,
                stateCandidate);
        }

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
            IDictionary<IPeerIdentifier, ICorrelationId> contactedNeighbour = default)
        {
            var peerParam = peer ?? PeerIdentifierHelper.GetPeerIdentifier(ByteUtil.GenerateRandomByteArray(32).ToString());
            var currentPeerNeighboursParam = currentPeersNeighbours ?? MockNeighbours();

            var expectedPnrParam = expectedPnr;
            var contactedNeighbourParam = contactedNeighbour ?? MockContactedNeighboursValuePairs();

            var subbedOriginator = Substitute.For<IHastingsOriginator>();
            
            subbedOriginator.UnResponsivePeers.Count.Returns(0);
            subbedOriginator.Peer.Returns(peerParam);
            subbedOriginator.CurrentPeersNeighbours.Returns(currentPeerNeighboursParam);
            subbedOriginator.ExpectedPnr.Returns(expectedPnrParam);
            subbedOriginator.UnResponsivePeers.Returns(contactedNeighbourParam);

            return subbedOriginator;
        }

        public static IHastingMemento MockSeedState(IPeerIdentifier ownNode, List<string> domains, IPeerSettings peerSettings)
        {
            return MockMemento(ownNode, MockDnsClient(peerSettings, domains)
               .GetSeedNodesFromDns(peerSettings.SeedServers).ToList());
        }
        
        public static IHastingMemento SubSeedState(IPeerIdentifier ownNode, List<string> domains, IPeerSettings peerSettings)
        {
            return SubMemento(ownNode, MockDnsClient(peerSettings, domains)
               .GetSeedNodesFromDns(peerSettings.SeedServers).ToList());
        }
        
        public static KeyValuePair<ICorrelationId, IPeerIdentifier> MockPnr(IPeerIdentifier peerIdentifier = default, ICorrelationId correlationId = default)
        {
            return new KeyValuePair<ICorrelationId, IPeerIdentifier>(correlationId ?? CorrelationId.GenerateCorrelationId(), peerIdentifier ?? PeerIdentifierHelper.GetPeerIdentifier("sender"));
        }
        
        public static IList<IPeerIdentifier> MockNeighbours(int amount = 5)
        {
            return Enumerable.Range(0, amount).Select(i =>
                PeerIdentifierHelper.GetPeerIdentifier(Helper.RandomString())).ToList();
        }

        public static IDictionary<IPeerIdentifier, ICorrelationId> MockUnResponsiveNeighbours(IEnumerable<IPeerIdentifier> neighbours = default)
        {
            if (neighbours == null || neighbours.Equals(default))
            {
                neighbours = MockNeighbours();
            }
            
            var mockUnResponsiveNeighboursList = new Dictionary<IPeerIdentifier, ICorrelationId>();
            
            (neighbours ?? throw new ArgumentNullException(nameof(neighbours))).ToList().ForEach(i =>
            {
                mockUnResponsiveNeighboursList.Add(i, null);
            });

            return mockUnResponsiveNeighboursList;
        }

        public static IDictionary<IPeerIdentifier, ICorrelationId> MockContactedNeighboursValuePairs(IEnumerable<IPeerIdentifier> neighbours = default)
        {
            if (neighbours == null || neighbours.Equals(default))
            {
                neighbours = MockNeighbours();
            }
            
            var mockContactedNeighboursList = new Dictionary<IPeerIdentifier, ICorrelationId>();
            
            (neighbours ?? throw new ArgumentNullException(nameof(neighbours))).ToList().ForEach(i =>
            {
                mockContactedNeighboursList.Add(i, CorrelationId.GenerateCorrelationId());
            });

            return mockContactedNeighboursList;
        }

        public static IHastingMemento MockMemento(IPeerIdentifier identifier = default, IEnumerable<IPeerIdentifier> neighbours = default)
        {
            var peerParam = identifier ?? PeerIdentifierHelper.GetPeerIdentifier(Helper.RandomString());
            var neighbourParam = neighbours ?? MockNeighbours();
            return new HastingMemento(peerParam, neighbourParam);
        }
        
        public static IHastingMemento SubMemento(IPeerIdentifier identifier = default, IEnumerable<IPeerIdentifier> neighbours = default)
        {
            var subbedMemento = Substitute.For<IHastingMemento>();
            subbedMemento.Peer.Returns(identifier ?? PeerIdentifierHelper.GetPeerIdentifier(Helper.RandomString()));
            subbedMemento.Neighbours.Returns(neighbours ?? MockNeighbours());

            return subbedMemento;
        }
        
        public static Stack<IHastingMemento> MockMementoHistory(Stack<IHastingMemento> state, int depth = 10)
        {
            state.Push(new HastingMemento(state.Last().Neighbours.RandomElement(), MockNeighbours()));

            return state.Count >= depth ? MockMementoHistory(state) : state;
        }
        
        public static IDns MockDnsClient(IPeerSettings settings,
            List<string> domains = default,
            string seedPid =
                "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839")
        {
            if (domains == default)
            {
                domains = Enumerable.Range(0, 5).Select(i => Helper.RandomString()).ToList();
            }
            
            domains.ForEach(domain =>
            {
                MockQueryResponse.CreateFakeLookupResult(domain, "0x" + PeerIdentifierHelper.GetPeerIdentifier(Helper.RandomString(32)).ToString().ToHexUTF8(), Substitute.For<ILookupClient>());
            });
        
            return new Common.Network.DevDnsClient(settings);
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
        
        public static IDtoFactory SubDtoFactory<T>(IPeerIdentifier sender,
            IDictionary<IPeerIdentifier, ICorrelationId> knownRequests,
            T message) where T : IMessage<T>
        {
            var subbedDtoFactory = Substitute.For<IDtoFactory>();
         
            knownRequests.ToList().ForEach(r =>
            {
                var (key, _) = r;
                subbedDtoFactory.GetDto(Arg.Any<T>(),
                    sender,
                    key
                ).Returns(
                    new MessageDto<T>(message, sender, key)
                );
            });

            return subbedDtoFactory;
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
