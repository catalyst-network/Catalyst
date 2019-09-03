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
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Consensus.Cycle;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Core.Consensus.Cycle;
using Microsoft.Reactive.Testing;
using Multiformats.Hash;
using NSubstitute;
using Serilog;

namespace Catalyst.TestUtils
{
    public class TestCycleEventProvider : ICycleEventsProvider, IDisposable
    {
        private readonly ICycleEventsProvider _cycleEventsProvider;
        private readonly IDisposable _deltaUpdatesSubscription;

        public TestCycleEventProvider(ILogger logger = null)
        {
            Scheduler = new TestScheduler();
            
            var schedulerProvider = Substitute.For<ICycleSchedulerProvider>();
            schedulerProvider.Scheduler.Returns(Scheduler);

            var dateTimeProvider = Substitute.For<IDateTimeProvider>();
            var deltaHashProvider = Substitute.For<IDeltaHashProvider>();

            dateTimeProvider.UtcNow.Returns(_ => Scheduler.Now.DateTime);

            _cycleEventsProvider = new CycleEventsProvider(
                CycleConfiguration.Default, dateTimeProvider, schedulerProvider, deltaHashProvider, logger ?? Substitute.For<ILogger>());

            deltaHashProvider.GetLatestDeltaHash(Arg.Any<DateTime>())
               .Returns(ci => Multihash.Sum(HashType.BLAKE2B_256, 
                    BitConverter.GetBytes(((DateTime) ci[0]).Ticks / (int) _cycleEventsProvider.Configuration.CycleDuration.Ticks)));

            _deltaUpdatesSubscription = PhaseChanges.Subscribe(p => CurrentPhase = p);
        }

        public ICycleConfiguration Configuration => _cycleEventsProvider.Configuration;
        public IObservable<IPhase> PhaseChanges => _cycleEventsProvider.PhaseChanges;
        public TimeSpan GetTimeSpanUntilNextCycleStart() => _cycleEventsProvider.GetTimeSpanUntilNextCycleStart();
        public void Close() { _cycleEventsProvider.Close(); }
        public TestScheduler Scheduler { get; }

        public IPhase CurrentPhase { get; private set; }

        public void MovePastNextPhase(PhaseName phaseName)
        {
            var offset = GetTimeSpanUntilNextCycleStart()
               .Add(Configuration.TimingsByName[phaseName].Offset)
               .Add(TimeSpan.FromTicks(100));

            Scheduler.AdvanceBy(offset.Ticks);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _deltaUpdatesSubscription?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
