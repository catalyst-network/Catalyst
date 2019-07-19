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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Core.Lib.UnitTests.P2P.Discovery
{
    public class DiscoveryTestBuilder : IDisposable
    {
        private int _burnIn;
        private bool _autoStart;
        private ILogger _logger;
        private IDns _dnsClient;
        private IPeerClient _peerClient;
        private IDtoFactory _dtoFactory;
        private IPeerSettings _peerSettings;
        private IHastingCareTaker _careTaker;
        private IHastingsOriginator _currentState;
        private IHastingsOriginator _stateCandidate;
        private ICancellationTokenProvider _cancellationProvider;
        internal IList<IPeerClientObservable> _peerClientObservables;
        private IPeerMessageCorrelationManager _peerCorrelationManager;
        private IRepository<Peer> _peerRepository;
        protected DiscoveryTestBuilder() { }

        public static DiscoveryTestBuilder GetDiscoveryTestBuilder()
        {
            return new DiscoveryTestBuilder();
        }

        public HastingDiscoveryTest Build()
        {
            return HastingDiscoveryTest.GetTestInstanceOfDiscovery(_logger,
                _peerRepository,
                _dnsClient,
                _peerSettings,
                _peerClient,
                _dtoFactory,
                _peerCorrelationManager,
                _cancellationProvider,
                _peerClientObservables,
                _autoStart,
                _burnIn,
                _currentState,
                _careTaker,
                _stateCandidate);
        }

        public DiscoveryTestBuilder WithLogger(ILogger logger = default)
        {
            _logger = logger ?? Substitute.For<ILogger>();
            return this;
        }

        public DiscoveryTestBuilder WithPeerRepository(IRepository<Peer> peerRepository = default, bool mock = false)
        {
            _peerRepository = peerRepository == default && mock == false
                ? Substitute.For<IRepository<Peer>>()
                : peerRepository == default
                    ? _peerRepository = DiscoveryHelper.MockPeerRepository()
                    : _peerRepository = peerRepository;
            
            return this;
        }
        
        public DiscoveryTestBuilder WithPeerSettings(IPeerSettings peerSettings = default)
        {
            _peerSettings = peerSettings ?? PeerSettingsHelper.TestPeerSettings();
            return this;
        }
        
        public DiscoveryTestBuilder WithDns(IDns dnsClient = default, bool mock = false, IPeerSettings peerSettings = default)
        {
            _dnsClient = dnsClient == default && mock == false
                ? Substitute.For<IDns>()
                : DiscoveryHelper.MockDnsClient(_peerSettings = _peerSettings == null && peerSettings == default
                    ? PeerSettingsHelper.TestPeerSettings()
                    : peerSettings);

            return this;
        }

        public DiscoveryTestBuilder WithPeerClient(IPeerClient peerClient = default)
        {
            _peerClient = peerClient ?? Substitute.For<IPeerClient>();
            return this;
        }

        public DiscoveryTestBuilder WithDtoFactory(IDtoFactory dtoFactory = default,
            IPeerIdentifier sender = default,
            IDictionary<IPeerIdentifier, ICorrelationId> knownRequests = default)
        {
            _dtoFactory = dtoFactory ?? Substitute.For<IDtoFactory>();
            
            return _dtoFactory == null ? throw new Exception("dtoFactory can't be null") : this;
        }

        public DiscoveryTestBuilder WithPeerMessageCorrelationManager(IReputationManager reputationManager = default,
            IMemoryCache memoryCache = default,
            IChangeTokenProvider changeTokenProvider = default,
            ILogger logger = default)
        {
            _peerCorrelationManager =
                DiscoveryHelper.MockCorrelationManager(reputationManager, memoryCache, changeTokenProvider, logger);
            return this;
        }

        public DiscoveryTestBuilder WithCancellationProvider(ICancellationTokenProvider cancellationTokenProvider = default)
        {
            _cancellationProvider = cancellationTokenProvider;
            return this;
        }
        
        public DiscoveryTestBuilder WithPeerClientObservables(ILogger logger = default, params Type[] clientObservers)
        {
            if (_peerClientObservables == null) 
            {
                _peerClientObservables = new List<IPeerClientObservable>();
            }

            foreach (var observerType in clientObservers)
            {
                _peerClientObservables.Add((IPeerClientObservable) Activator.CreateInstance(observerType, logger ?? Substitute.For<ILogger>()));
            }

            return this;
        }

        public DiscoveryTestBuilder WithAutoStart(bool autoStart = false)
        {
            _autoStart = autoStart;
            return this;
        }

        public DiscoveryTestBuilder WithBurn(int burnInPeriod = 0)
        {
            _burnIn = burnInPeriod;
            return this;
        }

        public DiscoveryTestBuilder WithCurrentState(IHastingsOriginator currentState = default,
            bool mock = false,
            IPeerIdentifier peer = default,
            IList<IPeerIdentifier> currentPeersNeighbours = default,
            KeyValuePair<ICorrelationId, IPeerIdentifier> expectedPnr = default,
            IDictionary<IPeerIdentifier, ICorrelationId> contactedNeighbour = default)
        {
            _currentState = 
                currentState == default && mock == false 
                    ? _currentState = DiscoveryHelper.SubOriginator(peer, currentPeersNeighbours, expectedPnr, contactedNeighbour) 
                    : currentState == default && mock == true 
                        ? _currentState = DiscoveryHelper.MockOriginator(peer, currentPeersNeighbours, expectedPnr, contactedNeighbour) 
                        : _currentState = currentState;
            
            return this;
        }
        
        public DiscoveryTestBuilder WithStateCandidate(IHastingsOriginator currentState = default,
            bool mock = false,
            IPeerIdentifier peer = default,
            IList<IPeerIdentifier> currentPeersNeighbours = default,
            KeyValuePair<ICorrelationId, IPeerIdentifier> expectedPnr = default,
            IDictionary<IPeerIdentifier, ICorrelationId> contactedNeighbour = default)
        {
            _stateCandidate = 
                currentState == default && mock == false 
                    ? _currentState = DiscoveryHelper.SubOriginator(peer, currentPeersNeighbours, expectedPnr, contactedNeighbour) 
                    : currentState == default && mock == true 
                        ? _currentState = DiscoveryHelper.MockOriginator(peer, currentPeersNeighbours, expectedPnr, contactedNeighbour) 
                        : _currentState = currentState;

            return this;
        }

        public DiscoveryTestBuilder WithCareTaker(IHastingCareTaker hastingCareTaker = default, IEnumerable<IHastingMemento> history = default)
        {
            _careTaker = hastingCareTaker ?? DiscoveryHelper.MockCareTaker(history);
            return this;
        }

        public sealed class HastingDiscoveryTest : HastingsDiscovery
        {
            internal static HastingDiscoveryTest GetTestInstanceOfDiscovery(ILogger logger,
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
                IHastingsOriginator stateCandidate = null)
            {
                return new HastingDiscoveryTest(logger,
                    peerRepository,
                    peerSettings,
                    dns,
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

            private HastingDiscoveryTest(ILogger logger = default,
                IRepository<Peer> peerRepository = default,
                IPeerSettings peerSettings = default,
                IDns dns = default,
                IPeerClient peerClient = default,
                IDtoFactory dtoFactory = default,
                IPeerMessageCorrelationManager peerMessageCorrelationManager = default,
                ICancellationTokenProvider cancellationTokenProvider = default,
                IEnumerable<IPeerClientObservable> peerClientObservables = default,
                bool autoStart = true,
                int peerDiscoveryBurnIn = 10,
                IHastingsOriginator state = default,
                IHastingCareTaker hastingCareTaker = default,
                IHastingsOriginator stateCandidate = null) 
                : base(logger ?? Substitute.For<ILogger>(),
                    peerRepository ?? Substitute.For<IRepository<Peer>>(),
                    dns ?? DiscoveryHelper.MockDnsClient(peerSettings),
                    peerSettings ?? PeerSettingsHelper.TestPeerSettings(),
                    peerClient ?? Substitute.For<IPeerClient>(),
                    dtoFactory ?? Substitute.For<IDtoFactory>(),
                    peerMessageCorrelationManager ?? DiscoveryHelper.MockCorrelationManager(),
                    cancellationTokenProvider ?? new CancellationTokenProvider(),
                    peerClientObservables,
                    autoStart,
                    peerDiscoveryBurnIn,
                    state ?? Substitute.For<IHastingsOriginator>(),
                    hastingCareTaker ?? Substitute.For<IHastingCareTaker>(),
                    stateCandidate) { }

            public new void WalkForward() { base.WalkForward(); }

            public new void WalkBack() { base.WalkBack(); }

            public new bool HasValidCandidate() { return base.HasValidCandidate(); }
        }

        public void Dispose()
        {
            _peerClient?.Dispose();
            _peerCorrelationManager?.Dispose();
        }
    }
}
