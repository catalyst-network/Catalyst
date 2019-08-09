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
using System.Reactive.Linq;
using System.Threading;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Modules.Consensus.Cycle;
using Serilog;

namespace Catalyst.Core.Lib.Modules.Consensus.Cycle
{
    /// <inheritdoc cref="ICycleEventsProvider"/>
    /// <inheritdoc cref="IDisposable"/>
    public class CycleEventsProvider : ICycleEventsProvider, IDisposable
    {
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public CycleEventsProvider(ICycleConfiguration configuration,
            IDateTimeProvider timeProvider,
            ICycleSchedulerProvider schedulerProvider,
            IDeltaHashProvider deltaHashProvider,
            ILogger logger)
        {
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
            
            Configuration = configuration;
            var scheduler = schedulerProvider.Scheduler;

            var constructionStatusChanges = StatefulPhase.GetStatusChangeObservable(
                PhaseName.Construction, Configuration.Construction, Configuration.CycleDuration, scheduler);

            var campaigningStatusChanges = StatefulPhase.GetStatusChangeObservable(
                PhaseName.Campaigning, Configuration.Campaigning, Configuration.CycleDuration, scheduler);

            var votingStatusChanges = StatefulPhase.GetStatusChangeObservable(
                PhaseName.Voting, Configuration.Voting, Configuration.CycleDuration, scheduler);

            var synchronisationStatusChanges = StatefulPhase.GetStatusChangeObservable(
                PhaseName.Synchronisation, Configuration.Synchronisation, Configuration.CycleDuration, scheduler);

            var synchronisationOffset = GetTimeSpanUntilNextCycleStart(timeProvider);

            PhaseChanges = constructionStatusChanges
               .Merge(campaigningStatusChanges, scheduler)
               .Merge(votingStatusChanges, scheduler)
               .Merge(synchronisationStatusChanges, scheduler)
               .Delay(TimeSpan.FromTicks(synchronisationOffset), scheduler)
               .Select(s => new Phase(deltaHashProvider.GetLatestDeltaHash(timeProvider.UtcNow), s.Name, s.Status, timeProvider.UtcNow))
               .Do(p => _logger.Debug("Current delta production phase {phase}", p))
               .TakeWhile(_ => !_cancellationTokenSource.IsCancellationRequested);
        }

        private long GetTimeSpanUntilNextCycleStart(IDateTimeProvider timeProvider)
        {
            var cycleDurationTicks = timeProvider.UtcNow.Ticks % Configuration.CycleDuration.Ticks;
            return cycleDurationTicks == 0
                ? 0 
                : Configuration.CycleDuration.Ticks - cycleDurationTicks;
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
