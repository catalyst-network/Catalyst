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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Consensus.Cycle;
using Catalyst.Abstractions.Consensus.Deltas;
using Serilog;

namespace Catalyst.Core.Consensus.Cycle
{
    /// <inheritdoc cref="ICycleEventsProvider"/>
    /// <inheritdoc cref="IDisposable"/>
    public class CycleEventsProvider : ICycleEventsProvider, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        protected readonly IScheduler Scheduler;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CycleEventsProvider(ICycleConfiguration configuration,
            IDateTimeProvider timeProvider,
            ICycleSchedulerProvider schedulerProvider,
            IDeltaHashProvider deltaHashProvider,
            ILogger logger)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            Configuration = configuration;
            Scheduler = schedulerProvider.Scheduler;

            var constructionStatusChanges = StatefulPhase.GetStatusChangeObservable(
                PhaseName.Construction, Configuration.Construction, Configuration.CycleDuration, Scheduler);

            var campaigningStatusChanges = StatefulPhase.GetStatusChangeObservable(
                PhaseName.Campaigning, Configuration.Campaigning, Configuration.CycleDuration, Scheduler);

            var votingStatusChanges = StatefulPhase.GetStatusChangeObservable(
                PhaseName.Voting, Configuration.Voting, Configuration.CycleDuration, Scheduler);

            var synchronisationStatusChanges = StatefulPhase.GetStatusChangeObservable(
                PhaseName.Synchronisation, Configuration.Synchronisation, Configuration.CycleDuration, Scheduler);

            _dateTimeProvider = timeProvider;
            var synchronisationOffset = GetTimeSpanUntilNextCycleStart();

            PhaseChanges = constructionStatusChanges
               .Merge(campaigningStatusChanges, Scheduler)
               .Merge(votingStatusChanges, Scheduler)
               .Merge(synchronisationStatusChanges, Scheduler)
               .Delay(synchronisationOffset, Scheduler)
               .Select(s => new Phase(deltaHashProvider.GetLatestDeltaHash(_dateTimeProvider.UtcNow), s.Name, s.Status, _dateTimeProvider.UtcNow))
               .Do(p => logger.Debug("Current delta production phase {phase}", p), 
                    exception => logger.Error(exception, "{PhaseChanges} stream failed and will stop producing cycle events.", nameof(PhaseChanges)),
                    () => logger.Debug("Stream {PhaseChanges} completed.", nameof(PhaseChanges)))
               .TakeWhile(_ => !_cancellationTokenSource.IsCancellationRequested);
        }

        public TimeSpan GetTimeSpanUntilNextCycleStart()
        {
            var cycleDurationTicks = _dateTimeProvider.UtcNow.Ticks % Configuration.CycleDuration.Ticks;
            var ticksUntilNextCycleStart = cycleDurationTicks == 0
                ? 0 
                : Configuration.CycleDuration.Ticks - cycleDurationTicks;
            return TimeSpan.FromTicks(ticksUntilNextCycleStart);
        }

        /// <inheritdoc />
        public ICycleConfiguration Configuration { get; }

        /// <inheritdoc />
        public IObservable<IPhase> PhaseChanges { get; }

        /// <inheritdoc />
        public void Close() { _cancellationTokenSource.Cancel(); }

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
