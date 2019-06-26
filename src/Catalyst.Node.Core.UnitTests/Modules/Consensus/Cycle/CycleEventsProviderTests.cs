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
using System.Reactive.Linq;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Modules.Consensus.Cycle;
using Catalyst.Node.Core.Modules.Consensus.Cycle;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus.Cycle
{
    public sealed class CycleEventsProviderTests : IDisposable
    {
        private static readonly PhaseStatus[] StatusesInOrder = {PhaseStatus.Producing, PhaseStatus.Collecting, PhaseStatus.Idle};
        private readonly TestScheduler _testScheduler;
        private readonly CycleEventsProvider _cycleProvider;
        private readonly IDisposable _subscription;
        private readonly IObserver<IPhase> _spy;

        public CycleEventsProviderTests(ITestOutputHelper output)
        {
            var output1 = output;
            _testScheduler = new TestScheduler();

            var schedulerProvider = Substitute.For<ICycleSchedulerProvider>();
            schedulerProvider.Scheduler.Returns(_testScheduler);
            var dateTimeProvider = Substitute.For<IDateTimeProvider>();

            dateTimeProvider.UtcNow.Returns(_ => _testScheduler.Now.DateTime);
            _cycleProvider = new CycleEventsProvider(CycleConfiguration.Default, dateTimeProvider, schedulerProvider);

            _spy = Substitute.For<IObserver<IPhase>>();

            var stopWatch = _testScheduler.StartStopwatch();

            _subscription = _cycleProvider.PhaseChanges.Take(50)
               .Subscribe(p =>
                {
                    output1.WriteLine($"{stopWatch.Elapsed.TotalSeconds} -- {p}");
                    _spy.OnNext(p);
                }, () =>
                {
                    output1.WriteLine($"completed after {stopWatch.Elapsed.TotalSeconds:g}");
                    _spy.OnCompleted();
                });
        }

        [Fact]
        public void PhaseChanges_Should_Complete_When_Stop_Is_Called()
        {
            var cancellationTime = CycleConfiguration.Default.CycleDuration
               .Add(CycleConfiguration.Default.Construction.ProductionTime.Divide(2));

            _testScheduler.Schedule(cancellationTime, _cycleProvider.Close);

            _testScheduler.Start();

            _spy.Received(1).OnCompleted();
            _spy.ReceivedCalls().Count(r => r.GetMethodInfo().Name == nameof(_spy.OnNext))
               .Should().Be(13, "the cycle has been stopped after the start of the second loop" +
                    "and before the end of the construction/production phase. A full cycle is 3*4 calls to " +
                    "OnNext, and the start of the second loop is 1.");
        }

        [Fact]
        public void Changes_Should_Happen_In_Time()
        {
            _testScheduler.Start();

            _spy.Received(1).OnCompleted();

            var receivedPhases = _spy.ReceivedCalls().Where(r => r.GetMethodInfo().Name == nameof(_spy.OnNext))
               .Select(c => (IPhase) c.GetArguments().Single()).OrderBy(p => p.UtcStartTime).ToList();
            receivedPhases.Count.Should().Be(50);

            foreach (var phaseName in Enumeration.GetAll<PhaseName>())
            {
                var received = receivedPhases.Where(p => p.Name == phaseName).ToList();
                CheckStatusChangesHappenedInOrder(received, receivedPhases[0].UtcStartTime);
            }
        }

        private void CheckStatusChangesHappenedInOrder(IList<IPhase> phases, DateTime eventsStartTime)
        {
            for (var i = 0; i < phases.Count; i++)
            {
                phases[i].Status.Should().Be(StatusesInOrder[i % 3]);

                var timeDiff = phases[i].UtcStartTime - eventsStartTime;

                var phaseTimings = CycleConfiguration.Default.TimingsByName[phases[i].Name];
                var fullCycleOffset = CycleConfiguration.Default.CycleDuration.Multiply(i / 3);

                var expectedDiff = phases[i].Status == PhaseStatus.Producing
                    ? fullCycleOffset + phaseTimings.Offset
                    : phases[i].Status == PhaseStatus.Collecting
                        ? fullCycleOffset + phaseTimings.Offset + phaseTimings.ProductionTime
                        : fullCycleOffset + phaseTimings.Offset + phaseTimings.TotalTime;

                var nl = Environment.NewLine;
                timeDiff.TotalSeconds.Should()
                   .BeApproximately(expectedDiff.TotalSeconds, 0.0001d,
                        $"phase details are " +
                        $"{nl}{phases[i]}" +
                        $"{nl}{nameof(timeDiff)}: {timeDiff}" +
                        $"{nl}{nameof(fullCycleOffset)}: {fullCycleOffset}" +
                        $"{nl}{nameof(phaseTimings.Offset)}: {phaseTimings.Offset}" +
                        $"{nl}{nameof(phaseTimings.ProductionTime)}: {phaseTimings.ProductionTime}" +
                        $"{nl}{nameof(phaseTimings.CollectionTime)}: {phaseTimings.CollectionTime}" +
                        $"{nl}");
            }
        }

        public void Dispose()
        {
            _cycleProvider?.Dispose();
            _subscription?.Dispose();
        }
    }
}
