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

using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Consensus.Cycle;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Enumerator;
using Catalyst.Core.Modules.Consensus.Cycle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Catalyst.Protocol.Deltas;
using Catalyst.Abstractions.Validators;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Modules.Kvm;
using Nethermind.Core;

namespace Catalyst.Core.Modules.Consensus
{
    /// <inheritdoc />
    public class CycleEventsProviderRaw : ICycleEventsProvider, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IDeltaCache _deltaCache;
        private readonly IValidatorSetStore _validatorSetStore;
        private readonly IKeySigner _keySigner;
        private readonly Address _kvmAddress;

        private readonly IList<IPhaseName> _orderedPhaseNamesByOffset;
        private readonly IList<IPhaseStatus> _orderedPhaseStatuses;
        private readonly IDictionary<IPhaseStatus, Func<IPhaseTimings, TimeSpan>> _phaseOffsetMappings;

        private readonly ReplaySubject<IPhase> _phaseChangesMessageSubject;
        public IObservable<IPhase> PhaseChanges { set; get; }

        public ICycleConfiguration Configuration { get; }

        private readonly ILogger _logger;

        public CycleEventsProviderRaw(ICycleConfiguration configuration,
            IDateTimeProvider dateTimeProvider,
            IDeltaHashProvider deltaHashProvider,
            IDeltaCache deltaCache,
            IValidatorSetStore validatorSetStore,
            IKeySigner keySigner,
            ILogger logger)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Configuration = configuration;
            _dateTimeProvider = dateTimeProvider;
            _deltaHashProvider = deltaHashProvider;
            _deltaCache = deltaCache;
            _validatorSetStore = validatorSetStore;
            _logger = logger;

            var privateKey = keySigner.GetPrivateKey(KeyRegistryTypes.DefaultKey);
            var publicKey = privateKey.GetPublicKey();
            _kvmAddress = publicKey.ToKvmAddress();

            _phaseChangesMessageSubject = new ReplaySubject<IPhase>();
            PhaseChanges = _phaseChangesMessageSubject.AsObservable();

            _orderedPhaseNamesByOffset = CreateAndOrderPhaseNamesByOffset(Configuration);
            _orderedPhaseStatuses = CreatePhaseStatuses();
            _phaseOffsetMappings = CreatePhaseOffsetMappings();

            //Idle phase status is not needed, it is to keep timing in RXObserver event cycles.
            _orderedPhaseStatuses.Remove(PhaseStatus.Idle);
            _phaseOffsetMappings.Remove(PhaseStatus.Idle);
        }

        /// <inheritdoc />
        public TimeSpan GetTimeSpanUntilNextCycleStart()
        {
            var cycleDurationTicks = _dateTimeProvider.UtcNow.Ticks % Configuration.CycleDuration.Ticks;
            var ticksUntilNextCycleStart = cycleDurationTicks == 0
                ? 0
                : Configuration.CycleDuration.Ticks - cycleDurationTicks;
            return TimeSpan.FromTicks(ticksUntilNextCycleStart);
        }

        /// <summary>
        /// Use this method to find what time the next production cycle will start.
        /// </summary>
        /// <returns>A DateTime representing the time until next delta production cycle starts.</returns>
        private DateTime GetDateUntilNextCycleStart()
        {
            return _dateTimeProvider.UtcNow.Add(GetTimeSpanUntilNextCycleStart());
        }

        /// <summary>
        /// Use this method to create and order all the PhaseNames
        /// </summary>
        /// <param name="configuration">The cycle configuration settings</param>
        /// <returns>A list of all the PhaseNames</returns>
        private static IList<IPhaseName> CreateAndOrderPhaseNamesByOffset(ICycleConfiguration configuration)
        {
            return configuration.TimingsByName.Keys.OrderBy(x => configuration.TimingsByName[x].Offset).ToList();
        }

        /// <summary>
        /// Use this method to create all the PhaseStatuses
        /// </summary>
        /// <returns>A list of all the PhaseStatus</returns>
        private static IList<IPhaseStatus> CreatePhaseStatuses()
        {
            return Enumeration.GetAll<PhaseStatus>().Cast<IPhaseStatus>().ToList();
        }

        /// <summary>
        /// Use this method to create all the PhaseStatus to PhaseTiming mappings.
        /// </summary>
        /// <returns>A dictionary of all the PhaseStatus to PhaseTiming mappings</returns>
        private static IDictionary<IPhaseStatus, Func<IPhaseTimings, TimeSpan>> CreatePhaseOffsetMappings()
        {
            return new Dictionary<IPhaseStatus, Func<IPhaseTimings, TimeSpan>>
            {
                { PhaseStatus.Producing, x => x.Offset },
                { PhaseStatus.Collecting, x => x.Offset + x.ProductionTime },
                { PhaseStatus.Idle, x => x.Offset + x.TotalTime }
            };
        }

        /// <inheritdoc />
        public Task StartAsync()
        {
            var eventCycleThread = new Thread(new ThreadStart(EventCycleLoopThread))
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };
            eventCycleThread.Start();
            return Task.CompletedTask;
        }

        /// <summary>
        /// EventCycle Thread to loop through each cycle continuously, we want time precision for each phase.
        /// </summary>
        private void EventCycleLoopThread()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var currentDeltaCid = _deltaHashProvider.GetLatestDeltaHash(_dateTimeProvider.UtcNow);
                _deltaCache.TryGetOrAddConfirmedDelta(currentDeltaCid, out Delta currentDelta);
                var currentDeltaHeight = currentDelta.DeltaNumber;

                StartCycle(++currentDeltaHeight);
            }

            _logger.Debug("Stream {PhaseChanges} completed.", nameof(PhaseChanges));
            _phaseChangesMessageSubject.OnCompleted();
            _phaseChangesMessageSubject.Dispose();
        }

        /// <summary>
        /// Each cycle will produce 8 phase events excluding 'idle' phase status: 4 phases * 2 phase statuses = 8 phase events.
        /// </summary>
        /// <param name="cycleNumber">The current cycle</param>
        private void StartCycle(long cycleNumber)
        {
            var phaseNumber = 0;
            var statusNumber = 0;

            var startTime = GetDateUntilNextCycleStart();
            var nextCycleStartTime = startTime.Add(Configuration.CycleDuration);

            var validators = _validatorSetStore.Get(cycleNumber);
            var isValidator = validators.GetValidators().Contains(_kvmAddress);

            _logger.Debug($"Event Provider Cycle {cycleNumber } Starting at: {startTime}");
            _logger.Debug($"Event Provider Cycle {cycleNumber + 1} Starting at: {nextCycleStartTime}");

            while (phaseNumber < _orderedPhaseNamesByOffset.Count && !_cancellationTokenSource.IsCancellationRequested)
            {
                var currentPhaseName = _orderedPhaseNamesByOffset[phaseNumber];
                var currentPhaseTiming = Configuration.TimingsByName[currentPhaseName];
                var currentPhaseStatus = _orderedPhaseStatuses[statusNumber];

                var phase = GetPhase(startTime, _phaseOffsetMappings[currentPhaseStatus](currentPhaseTiming), currentPhaseName, currentPhaseStatus);
                if (phase != null)
                {
                    _logger.Debug("Current delta production phase {phase}", phase);
                    if (isValidator)
                    {
                        _phaseChangesMessageSubject.OnNext(phase);
                    }
                    statusNumber++;
                }

                if (statusNumber >= _orderedPhaseStatuses.Count)
                {
                    statusNumber = 0;
                    phaseNumber++;
                }

                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Find's the current phase after the specified time in the cycle.
        /// </summary>
        /// <param name="cycleStartTime">The cycle start time.</param>
        /// <param name="phaseOffset">The offset the phase will run in the cycle.</param>
        /// <param name="phaseName">The name of the phase.</param>
        /// <param name="phaseStatus">The phase status</param>
        /// <returns>The phase with the corresponding data.</returns>
        private IPhase GetPhase(DateTime cycleStartTime, TimeSpan phaseOffset, IPhaseName phaseName, IPhaseStatus phaseStatus)
        {
            var phaseStartTime = cycleStartTime.Add(phaseOffset);
            if (_dateTimeProvider.UtcNow.Ticks >= phaseStartTime.Ticks)
            {
                return new Phase(_deltaHashProvider.GetLatestDeltaHash(_dateTimeProvider.UtcNow), phaseName, phaseStatus, _dateTimeProvider.UtcNow);
            }

            return null;
        }

        /// <inheritdoc />
        public void Close()
        {
            _cancellationTokenSource.Cancel();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                Close();
            }

            _cancellationTokenSource.Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
