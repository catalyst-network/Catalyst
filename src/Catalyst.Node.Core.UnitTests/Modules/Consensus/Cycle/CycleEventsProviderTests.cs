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
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Modules.Consensus.Cycle;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Node.Core.Modules.Consensus.Cycle;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus.Cycle
{
    public class CycleEventsProviderTests
    {
        private readonly ITestOutputHelper _output;
        private static readonly PhaseStatus[] StatusesInOrder = new[] {PhaseStatus.Producing, PhaseStatus.Collecting, PhaseStatus.Idle};

        public CycleEventsProviderTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public async Task Changes_Should_Happen_In_Time()
        {
            var cycleProvider = new CycleEventsProvider(TestCycleConfiguration.TestDefault, new DateTimeProvider());

            var phaseChanges = cycleProvider.PhaseChanges;
            var completed = false;
            var receivedCount = 0;
            var spy = Substitute.For<IObserver<IPhase>>();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            _output.WriteLine($"starting at {stopWatch.Elapsed:g}");

            phaseChanges.SubscribeOn(TaskPoolScheduler.Default)
               .Subscribe(p =>
            {
                _output.WriteLine($"{stopWatch.Elapsed:g} -- {p}");
                receivedCount++;
                spy.OnNext(p);
            }, () =>
            {
                _output.WriteLine($"completed after {stopWatch.Elapsed:g}");
                completed = true;
                spy.OnCompleted();
            });

            await TaskHelper.WaitForAsync(() => receivedCount >= 20, TimeSpan.FromSeconds(100));
            cycleProvider.Close();

            await TaskHelper.WaitForAsync(() => completed, TimeSpan.FromSeconds(10));

            spy.Received(1).OnCompleted();

            var receivedPhases = spy.ReceivedCalls().Where(r => r.GetMethodInfo().Name == nameof(spy.OnNext))
               .Select(c => (IPhase) c.GetArguments().Single()).OrderBy(p => p.UtcStartTime).ToList();

            foreach (var phaseName in Enumeration.GetAll<PhaseName>())
            {
                var received = receivedPhases.Where(p => p.Name == phaseName).ToList();
                CheckStatusChangesHappenedInOrder(received, receivedPhases[0].UtcStartTime);
            }

            stopWatch.Stop();
        }

        private void CheckStatusChangesHappenedInOrder(IList<IPhase> phases, DateTime eventsStartTime)
        {
            var cycleDurationMs = TestCycleConfiguration.TestDefault.CycleDuration.TotalMilliseconds;
            var fivePercentTolerance = cycleDurationMs / 20d;
            fivePercentTolerance.Should().BeLessOrEqualTo(0.05d * cycleDurationMs, 
                "we can tolerate 5% error with respect to the total cycle duration. In a non testing context the " +
                $"duration will be {TestCycleConfiguration.CompressionFactor} times longer, but the errors will stay in the same " +
                $"range, so we end up with a prod tolerance of {0.05 / TestCycleConfiguration.CompressionFactor:P1} of error.");

            for (var i = 0; i < phases.Count; i++)
            {
                phases[i].Status.Should().Be(StatusesInOrder[i % 3]);

                var timeDiff = phases[i].UtcStartTime - eventsStartTime;

                var phaseTimings = TestCycleConfiguration.TestDefault.TimingsByName[phases[i].Name];
                var fullCycleOffset = TestCycleConfiguration.TestDefault.CycleDuration.Multiply(i / 3);

                var expectedDiff = phases[i].Status == PhaseStatus.Producing
                    ? fullCycleOffset + phaseTimings.Offset
                    : phases[i].Status == PhaseStatus.Collecting
                        ? fullCycleOffset + phaseTimings.Offset + phaseTimings.ProductionTime
                        : fullCycleOffset + phaseTimings.Offset + phaseTimings.TotalTime;

                var _ = Environment.NewLine;
                timeDiff.TotalMilliseconds.Should()
                   .BeApproximately(expectedDiff.TotalMilliseconds, fivePercentTolerance, 
                        $"{_}{phases[i]}" +
                        $"{_}{nameof(timeDiff)}: {timeDiff}" +
                        $"{_}{nameof(fullCycleOffset)}: {fullCycleOffset}" +
                        $"{_}{nameof(phaseTimings.Offset)}: {phaseTimings.Offset}" +
                        $"{_}{nameof(phaseTimings.ProductionTime)}: {phaseTimings.ProductionTime}" +
                        $"{_}{nameof(phaseTimings.CollectionTime)}: {phaseTimings.CollectionTime}" +
                        $"{_}");
            }
        }
    }
}
