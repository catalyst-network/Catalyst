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
using System.Threading.Tasks;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Modules.Consensus.Cycle;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Node.Core.Modules.Consensus.Cycle;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Org.BouncyCastle.Bcpg;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus.Cycle
{
    public class CycleEventsProviderTests
    {
        private readonly ITestOutputHelper _output;
        private static readonly PhaseStatus[] StatusesInOrder = new[] {PhaseStatus.Producing, PhaseStatus.Collecting, PhaseStatus.Idle};

        public CycleEventsProviderTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public async Task Changes_should_happen_in_time()
        {
            _output.WriteLine($"starting at {DateTime.Now:ss:fff}");

            var cycleProvider = new CycleEventsProvider(TestCycleConfiguration.TestDefault, new DateTimeProvider());

            var phaseChanges = cycleProvider.PhaseChanges;
            var completed = false;
            var receivedCount = 0;
            var spy = Substitute.For<IObserver<IPhase>>();

            phaseChanges.Subscribe(p =>
            {
                _output.WriteLine($"{DateTime.Now:ss:fff} -- {p}");
                receivedCount++;
                spy.OnNext(p);
            }, () =>
            {
                _output.WriteLine($"completed at {DateTime.Now:ss:fff}");
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
                CheckStatusChangesHappenedInOrder(received);
            }
        }

        private void CheckStatusChangesHappenedInOrder(IList<IPhase> phases)
        {
            phases.Select((p, i) => p.Status == StatusesInOrder[i % StatusesInOrder.Length])
               .Should().AllBeEquivalentTo(true);

            for (var i = 1; i < phases.Count; i++)
            {
                var timeDiff = phases[i].UtcStartTime - phases[0].UtcStartTime;

                var phaseTimings = TestCycleConfiguration.TestDefault.TimingsByName[phases[i].Name];
                var fullCycleOffset = TestCycleConfiguration.TestDefault.CycleDuration.Multiply(i % 3);

                var expectedDiff = (phases[i].Status == PhaseStatus.Producing)
                    ? fullCycleOffset + phaseTimings.Offset
                    : phases[i].Status == PhaseStatus.Collecting
                        ? fullCycleOffset + phaseTimings.ProductionTime
                        : fullCycleOffset + phaseTimings.TotalTime;

                timeDiff.TotalMilliseconds.Should()
                   .BeApproximately(expectedDiff.TotalMilliseconds, 1d);
            }
        }
    }
}
