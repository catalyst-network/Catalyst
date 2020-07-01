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

namespace Catalyst.Core.Modules.Consensus
{
    public class CycleEventsProviderRaw : ICycleEventsProvider, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IDeltaHashProvider _deltaHashProvider;

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
            ILogger logger)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Configuration = configuration;
            _dateTimeProvider = dateTimeProvider;
            _deltaHashProvider = deltaHashProvider;
            _logger = logger;

            _phaseChangesMessageSubject = new ReplaySubject<IPhase>();
            PhaseChanges = _phaseChangesMessageSubject.AsObservable();

            _orderedPhaseNamesByOffset = CreateAndOrderPhaseNamesByOffset();
            _orderedPhaseStatuses = CreateAndOrderPhaseStatuses();
            _phaseOffsetMappings = CreatePhaseOffsetMappings();

            //Idle phase status is not needed, it is to keep timing in RXObserver event cycles.
            _orderedPhaseStatuses.Remove(PhaseStatus.Idle);
            _phaseOffsetMappings.Remove(PhaseStatus.Idle);
        }

        public TimeSpan GetTimeSpanUntilNextCycleStart()
        {
            var cycleDurationTicks = _dateTimeProvider.UtcNow.Ticks % Configuration.CycleDuration.Ticks;
            var ticksUntilNextCycleStart = cycleDurationTicks == 0
                ? 0
                : Configuration.CycleDuration.Ticks - cycleDurationTicks;
            return TimeSpan.FromTicks(ticksUntilNextCycleStart);
        }

        private IList<IPhaseName> CreateAndOrderPhaseNamesByOffset()
        {
            return Configuration.TimingsByName.Keys.OrderBy(x => Configuration.TimingsByName[x].Offset).ToList();
        }

        private IList<IPhaseStatus> CreateAndOrderPhaseStatuses()
        {
            return Enumeration.GetAll<PhaseStatus>().Cast<IPhaseStatus>().ToList();
        }

        private IDictionary<IPhaseStatus, Func<IPhaseTimings, TimeSpan>> CreatePhaseOffsetMappings()
        {
            var phaseOffsetMappings = new Dictionary<IPhaseStatus, Func<IPhaseTimings, TimeSpan>>();
            phaseOffsetMappings.Add(PhaseStatus.Producing, x => x.Offset);
            phaseOffsetMappings.Add(PhaseStatus.Collecting, x => x.Offset + x.ProductionTime);
            phaseOffsetMappings.Add(PhaseStatus.Idle, x => x.Offset + x.TotalTime);
            return phaseOffsetMappings;
        }

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

        private void EventCycleLoopThread()
        {
            var cycleNumber = 1UL;
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                StartCycle(cycleNumber++);
            }

            _logger.Debug("Stream {PhaseChanges} completed.", nameof(PhaseChanges));
            _phaseChangesMessageSubject.OnCompleted();
        }

        private void StartCycle(ulong cycleNumber)
        {
            var phaseNumber = 0;
            var statusNumber = 0;

            var startTime = _dateTimeProvider.GetDateUntilNextCycleStart(Configuration.CycleDuration);
            var nextCycleStartTime = startTime.Add(Configuration.CycleDuration);

            _logger.Debug($"Event Provider Cycle {cycleNumber} Starting at: {startTime}");
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
                    _phaseChangesMessageSubject.OnNext(phase);
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

        private IPhase GetPhase(DateTime cycleStartTime, TimeSpan phaseOffset, IPhaseName phaseName, IPhaseStatus phaseStatus)
        {
            var phaseStartTime = cycleStartTime.Add(phaseOffset);
            if (_dateTimeProvider.UtcNow.Ticks >= phaseStartTime.Ticks)
            {
                return new Phase(_deltaHashProvider.GetLatestDeltaHash(_dateTimeProvider.UtcNow), phaseName, phaseStatus, _dateTimeProvider.UtcNow);
            }

            return null;
        }

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

            _phaseChangesMessageSubject.Dispose();
            _cancellationTokenSource.Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
