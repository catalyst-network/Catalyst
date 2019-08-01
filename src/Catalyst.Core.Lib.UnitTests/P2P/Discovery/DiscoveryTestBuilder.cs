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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.TestUtils;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Core.Lib.UnitTests.P2P.Discovery
{
    public sealed class DiscoveryTestBuilder : IDisposable
    {
        private int _burnIn;
        private bool _autoStart;
        private ILogger _logger;
        private IDns _dnsClient;
        private IPeerClient _peerClient;
        private IPeerSettings _peerSettings;
        private IHastingsCareTaker _careTaker;
        private IHastingsOriginator _currentState;
        private ICancellationTokenProvider _cancellationProvider;
        public IList<IPeerClientObservable> PeerClientObservables;
        private IPeerMessageCorrelationManager _peerCorrelationManager;
        private IRepository<Peer> _peerRepository;
        private int _timeout;

        public HastingDiscoveryTest Build()
        {
            return HastingDiscoveryTest.GetTestInstanceOfDiscovery(_logger,
                _peerRepository,
                _dnsClient,
                _peerSettings,
                _peerClient,
                _peerCorrelationManager,
                _cancellationProvider,
                PeerClientObservables,
                _autoStart,
                _burnIn,
                _currentState,
                _careTaker,
                _timeout);
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

        public DiscoveryTestBuilder WithTimeout(int timeoutMilliseconds)
        {
            _timeout = timeoutMilliseconds;
            
            return this;
        }

        public DiscoveryTestBuilder WithPeerMessageCorrelationManager(IPeerMessageCorrelationManager peerMessageCorrelationManager = default,
            IReputationManager reputationManager = default,
            IMemoryCache memoryCache = default,
            IChangeTokenProvider changeTokenProvider = default)
        {
            _peerCorrelationManager = peerMessageCorrelationManager ??
                DiscoveryHelper.MockCorrelationManager(reputationManager, memoryCache, changeTokenProvider, _logger);
            
            return this;
        }

        public DiscoveryTestBuilder WithCancellationProvider(ICancellationTokenProvider cancellationTokenProvider = default)
        {
            _cancellationProvider = cancellationTokenProvider;
            return this;
        }
        
        public DiscoveryTestBuilder WithPeerClientObservables(params Type[] clientObservers)
        {
            PeerClientObservables = clientObservers
               .Select(ot => (IPeerClientObservable) Activator.CreateInstance(ot, _logger ?? Substitute.For<ILogger>()))
               .ToList();
            
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

        public DiscoveryTestBuilder WithCurrentStep(IHastingsMemento currentStep = default,
            bool mock = false,
            IPeerIdentifier peer = default,
            INeighbours neighbours = default)
        {
            if (_careTaker == null)
            {
                WithCareTaker();
            }

            var memento = mock
                ? currentStep ?? DiscoveryHelper.MockMemento(peer, neighbours)
                : currentStep ?? DiscoveryHelper.SubMemento(peer, neighbours);

            _careTaker.Add(memento);
            
            return this;
        }
        
        public DiscoveryTestBuilder WithStepProposal(IHastingsOriginator stateCandidate = default,
            bool mock = false,
            IPeerIdentifier peer = default,
            INeighbours neighbours = default,
            ICorrelationId expectedPnr = default)
        {
            var pnrCorrelationId = expectedPnr ?? CorrelationId.GenerateCorrelationId();

            _currentState = mock
                ? stateCandidate ?? DiscoveryHelper.MockOriginator(peer, neighbours)
                : stateCandidate ?? DiscoveryHelper.SubOriginator(peer, neighbours, pnrCorrelationId);

            return this;
        }

        public DiscoveryTestBuilder WithCareTaker(IHastingsCareTaker hastingsCareTaker = default, IEnumerable<IHastingsMemento> history = default)
        {
            _careTaker = hastingsCareTaker ?? DiscoveryHelper.MockCareTaker(history);
            return this;
        }

        public sealed class HastingDiscoveryTest : HastingsDiscovery
        {
            internal static HastingDiscoveryTest GetTestInstanceOfDiscovery(ILogger logger,
                IRepository<Peer> peerRepository,
                IDns dns,
                IPeerSettings peerSettings,
                IPeerClient peerClient,
                IPeerMessageCorrelationManager peerMessageCorrelationManager,
                ICancellationTokenProvider cancellationTokenProvider,
                IEnumerable<IPeerClientObservable> peerClientObservables,
                bool autoStart = true,
                int peerDiscoveryBurnIn = 10,
                IHastingsOriginator state = default,
                IHastingsCareTaker hastingsCareTaker = default,
                int millisecondsTimeout = 10_000)
            {
                return new HastingDiscoveryTest(logger,
                    peerRepository,
                    peerSettings,
                    dns,
                    peerClient,
                    peerMessageCorrelationManager,
                    cancellationTokenProvider,
                    peerClientObservables,
                    autoStart,
                    peerDiscoveryBurnIn,
                    state,
                    hastingsCareTaker,
                    millisecondsTimeout);
            }

            private HastingDiscoveryTest(ILogger logger = default,
                IRepository<Peer> peerRepository = default,
                IPeerSettings peerSettings = default,
                IDns dns = default,
                IPeerClient peerClient = default,
                IPeerMessageCorrelationManager peerMessageCorrelationManager = default,
                ICancellationTokenProvider cancellationTokenProvider = default,
                IEnumerable<IPeerClientObservable> peerClientObservables = default,
                bool autoStart = true,
                int peerDiscoveryBurnIn = 10,
                IHastingsOriginator state = default,
                IHastingsCareTaker hastingsCareTaker = default,
                int millisecondsTimeout = 10_000)
                : base(logger ?? Substitute.For<ILogger>(),
                    peerRepository ?? Substitute.For<IRepository<Peer>>(),
                    dns ?? DiscoveryHelper.MockDnsClient(peerSettings),
                    peerSettings ?? PeerSettingsHelper.TestPeerSettings(),
                    peerClient ?? Substitute.For<IPeerClient>(),
                    Substitute.For<IDtoFactory>(),
                    peerMessageCorrelationManager ?? DiscoveryHelper.MockCorrelationManager(),
                    cancellationTokenProvider ?? new CancellationTokenProvider(),
                    peerClientObservables,
                    autoStart,
                    peerDiscoveryBurnIn,
                    state,
                    hastingsCareTaker,
                    millisecondsTimeout) { }

            internal new void WalkForward() { base.WalkForward(); }

            public new void WalkBack() { base.WalkBack(); }

            internal int GetBurnInValue() { return PeerDiscoveryBurnIn; }

            internal void TestStorePeer(INeighbour neighbour) { StorePeer(neighbour); }

            public void TestEvictionCallback(ICorrelationId item)
            {
                EvictionCallback(item);
            }
        }

        public void Dispose()
        {
            _peerClient?.Dispose();
            _peerCorrelationManager?.Dispose();
            _peerRepository?.Dispose();
        }
    }
}
