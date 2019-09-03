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
using System.Reflection;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Network;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Abstractions.Util;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.P2P.Discovery.Hastings;
using Catalyst.Core.P2P.IO.Observers;
using Catalyst.Core.P2P.Models;
using Catalyst.Core.P2P.ReputationSystem;
using Catalyst.Core.Util;
using Catalyst.TestUtils;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Core.UnitTests.P2P.Discovery
{
    public sealed class DiscoveryTestBuilder : IDisposable
    {
        private bool _autoStart;
        private int _burnIn;
        private ICancellationTokenProvider _cancellationProvider;
        private IHastingsCareTaker _careTaker;
        private IHastingsOriginator _currentState;
        private IDns _dnsClient;
        private int _hasValidCandidatesCheckMillisecondsFrequency;
        private ILogger _logger;
        private IPeerClient _peerClient;
        private IPeerMessageCorrelationManager _peerCorrelationManager;
        private IRepository<Peer> _peerRepository;
        private IPeerSettings _peerSettings;
        private IScheduler _scheduler;
        private int _timeout;
        public IList<IPeerClientObservable> PeerClientObservables;

        public void Dispose()
        {
            _peerClient?.Dispose();
            _peerCorrelationManager?.Dispose();
            _peerRepository?.Dispose();
        }

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
                _timeout,
                _hasValidCandidatesCheckMillisecondsFrequency,
                _scheduler);
        }

        public DiscoveryTestBuilder WithScheduler(IScheduler scheduler = null)
        {
            _scheduler = scheduler ?? Scheduler.Default;
            return this;
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

        public DiscoveryTestBuilder WithDns(IDns dnsClient = default,
            bool mock = false,
            IPeerSettings peerSettings = default)
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

        public DiscoveryTestBuilder WithCandidatesCheckMillisecondsFrequency(int candidatesCheckMillisecondsFrequency)
        {
            _hasValidCandidatesCheckMillisecondsFrequency = candidatesCheckMillisecondsFrequency;

            return this;
        }

        public DiscoveryTestBuilder WithPeerMessageCorrelationManager(IPeerMessageCorrelationManager peerMessageCorrelationManager = default,
            IReputationManager reputationManager = default,
            IMemoryCache memoryCache = default,
            IChangeTokenProvider changeTokenProvider = default)
        {
            _peerCorrelationManager = peerMessageCorrelationManager ??
                DiscoveryHelper.MockCorrelationManager(_scheduler, reputationManager, memoryCache, changeTokenProvider,
                    _logger);

            return this;
        }

        public DiscoveryTestBuilder WithCancellationProvider(ICancellationTokenProvider cancellationTokenProvider =
            default)
        {
            _cancellationProvider = cancellationTokenProvider;
            return this;
        }

        public DiscoveryTestBuilder WithPeerClientObservables(params Type[] clientObservers)
        {
            PeerClientObservables = clientObservers
               .Select(ot => GetPeerClientObservable(_logger ?? Substitute.For<ILogger>(), ot))
               .ToList();

            return this;
        }

        private IPeerClientObservable GetPeerClientObservable(ILogger logger, MemberInfo type)
        {
            switch (type.Name)
            {
                case nameof(PingResponseObserver):
                    return new PingResponseObserver(logger, Substitute.For<IPeerChallenger>());

                case nameof(GetNeighbourResponseObserver):
                    return new GetNeighbourResponseObserver(logger);

                default:
                    throw new NotImplementedException($"{type.Name} type not supported.");
            }
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

        public DiscoveryTestBuilder WithCareTaker(IHastingsCareTaker hastingsCareTaker = default,
            IEnumerable<IHastingsMemento> history = default)
        {
            _careTaker = hastingsCareTaker ?? DiscoveryHelper.MockCareTaker(history);
            return this;
        }

        public sealed class HastingDiscoveryTest : HastingsDiscovery
        {
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
                int millisecondsTimeout = 10_000,
                int hasValidCandidatesCheckMillisecondsFrequency = 1_000,
                IScheduler scheduler = null)
                : base(logger ?? Substitute.For<ILogger>(),
                    peerRepository ?? Substitute.For<IRepository<Peer>>(),
                    dns ?? DiscoveryHelper.MockDnsClient(peerSettings),
                    peerSettings ?? PeerSettingsHelper.TestPeerSettings(),
                    peerClient ?? Substitute.For<IPeerClient>(),
                    peerMessageCorrelationManager ?? DiscoveryHelper.MockCorrelationManager(scheduler),
                    cancellationTokenProvider ?? new CancellationTokenProvider(),
                    peerClientObservables,
                    autoStart,
                    peerDiscoveryBurnIn,
                    state,
                    hastingsCareTaker,
                    millisecondsTimeout,
                    hasValidCandidatesCheckMillisecondsFrequency) { }

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
                int millisecondsTimeout = 10_000,
                int hasValidCandidatesCheckMillisecondsFrequency = 1_000,
                IScheduler scheduler = null)
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
                    millisecondsTimeout,
                    hasValidCandidatesCheckMillisecondsFrequency,
                    scheduler);
            }

            internal new void WalkForward() { base.WalkForward(); }

            public new void WalkBack() { base.WalkBack(); }

            internal int GetBurnInValue() { return PeerDiscoveryBurnIn; }

            internal void TestStorePeer(INeighbour neighbour) { StorePeer(neighbour); }

            public void TestEvictionCallback(ICorrelationId item) { EvictionCallback(item); }
        }
    }
}
