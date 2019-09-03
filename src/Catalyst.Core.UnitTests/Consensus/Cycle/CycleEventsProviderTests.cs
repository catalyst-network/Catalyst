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
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Consensus.Cycle;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Enumerator;
using Catalyst.Core.Consensus.Cycle;
using Catalyst.Core.Util;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Multiformats.Hash;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.UnitTests.Consensus.Cycle
{
    public sealed class CycleEventsProviderTests : IDisposable
    {
        private const int PhaseCountPerCycle = 12;
        private static readonly PhaseStatus[] StatusesInOrder = {PhaseStatus.Producing, PhaseStatus.Collecting, PhaseStatus.Idle};
        private readonly TestScheduler _testScheduler;
        private readonly CycleEventsProvider _cycleProvider;
        private readonly IDisposable _subscription;
        private readonly IObserver<IPhase> _spy;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ICycleSchedulerProvider _schedulerProvider;
        private readonly ITestOutputHelper _output;
        private readonly IStopwatch _stopWatch;
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly ILogger _logger;

        public CycleEventsProviderTests(ITestOutputHelper output)
        {
            _output = output;
            _testScheduler = new TestScheduler();

            _schedulerProvider = Substitute.For<ICycleSchedulerProvider>();
            _schedulerProvider.Scheduler.Returns(_testScheduler);
            _dateTimeProvider = Substitute.For<IDateTimeProvider>();
            _deltaHashProvider = Substitute.For<IDeltaHashProvider>();
            _logger = Substitute.For<ILogger>();

            _deltaHashProvider.GetLatestDeltaHash(Arg.Any<DateTime>())
               .Returns(Multihash.Sum(HashType.ID, ByteUtil.GenerateRandomByteArray(32)));

            _dateTimeProvider.UtcNow.Returns(_ => _testScheduler.Now.DateTime);
            _cycleProvider = new CycleEventsProvider(CycleConfiguration.Default, _dateTimeProvider, _schedulerProvider, _deltaHashProvider, _logger);

            _spy = Substitute.For<IObserver<IPhase>>();

            _stopWatch = _testScheduler.StartStopwatch();

            _subscription = _cycleProvider.PhaseChanges.Take(50)
               .Subscribe(p =>
                {
                    _output.WriteLine($"{_stopWatch.Elapsed.TotalSeconds} -- {p}");
                    _spy.OnNext(p);
                }, () =>
                {
                    _output.WriteLine($"completed after {_stopWatch.Elapsed.TotalSeconds:g}");
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
               .Should().Be(PhaseCountPerCycle + 1, "the cycle has been stopped after the start of the second loop" +
                    "and before the end of the construction/production phase. A full cycle is 3*4 calls to " +
                    "OnNext, and the start of the second loop is 1.");
        }

        [Fact]
        public void Changes_Should_Happen_In_Time()
        {
            _testScheduler.Start();

            _spy.Received(1).OnCompleted();

            var receivedPhases = GetReceivedPhases(_spy);
            receivedPhases.Count.Should().Be(50);

            foreach (var phaseName in Enumeration.GetAll<PhaseName>())
            {
                var received = receivedPhases.Where(p => p.Name.Equals(phaseName)).ToList();
                CheckStatusChangesHappenedInOrder(received, receivedPhases[0].UtcStartTime);
            }
        }

        private List<IPhase> GetReceivedPhases(IObserver<IPhase> observer)
        {
            return observer.ReceivedCalls().Where(r => r.GetMethodInfo().Name == nameof(observer.OnNext))
               .Select(c => (IPhase) c.GetArguments().Single()).OrderBy(p => p.UtcStartTime).ToList();
        }

        private void CheckStatusChangesHappenedInOrder(IList<IPhase> phases, DateTime eventsStartTime)
        {
            for (var i = 0; i < phases.Count; i++)
            {
                phases[i].Status.Should().Be(StatusesInOrder[i % 3]);

                var timeDiff = phases[i].UtcStartTime - eventsStartTime;

                var phaseTimings = CycleConfiguration.Default.TimingsByName[phases[i].Name];
                var fullCycleOffset = CycleConfiguration.Default.CycleDuration.Multiply(i / 3);

                var expectedDiff = phases[i].Status.Equals(PhaseStatus.Producing)
                    ? fullCycleOffset + phaseTimings.Offset
                    : phases[i].Status.Equals(PhaseStatus.Collecting)
                        ? fullCycleOffset + phaseTimings.Offset + phaseTimings.ProductionTime
                        : fullCycleOffset + phaseTimings.Offset + phaseTimings.TotalTime;

                var nl = Environment.NewLine;
                timeDiff.TotalSeconds.Should()
                   .BeApproximately(expectedDiff.TotalSeconds, 0.0001d,
                        "phase details are " +
                        $"{nl}{phases[i]}" +
                        $"{nl}{nameof(timeDiff)}: {timeDiff.ToString()}" +
                        $"{nl}{nameof(fullCycleOffset)}: {fullCycleOffset.ToString()}" +
                        $"{nl}{nameof(phaseTimings.Offset)}: {phaseTimings.Offset.ToString()}" +
                        $"{nl}{nameof(phaseTimings.ProductionTime)}: {phaseTimings.ProductionTime.ToString()}" +
                        $"{nl}{nameof(phaseTimings.CollectionTime)}: {phaseTimings.CollectionTime.ToString()}" +
                        $"{nl}");
            }
        }

        [Fact]
        public void PhaseChanges_Should_Be_Synchronised_Across_Instances()
        {
            var secondProviderStartOffset = CycleConfiguration.Default.CycleDuration.Divide(3);

            _testScheduler.AdvanceBy(secondProviderStartOffset.Ticks);

            var spy2 = Substitute.For<IObserver<IPhase>>();
            using (var cycleProvider2 = new CycleEventsProvider(CycleConfiguration.Default, _dateTimeProvider, _schedulerProvider, _deltaHashProvider, _logger))
            using (cycleProvider2.PhaseChanges.Take(50 - PhaseCountPerCycle)
               .Subscribe(p =>
                {
                    _output.WriteLine($"{_stopWatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)} % 2 -- {p}");
                    spy2.OnNext(p);
                }, () =>
                {
                    _output.WriteLine($"% 2 -- completed after {_stopWatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture):g}");
                    spy2.OnCompleted();
                }))
            {
                _testScheduler.Start();

                _spy.Received(1).OnCompleted();
                spy2.Received(1).OnCompleted();

                var receivedPhases = GetReceivedPhases(_spy);
                receivedPhases.Count.Should().Be(50);

                var receivedPhases2 = GetReceivedPhases(spy2);
                receivedPhases2.Count.Should().Be(50 - PhaseCountPerCycle);

                (receivedPhases2.First().UtcStartTime - receivedPhases.First().UtcStartTime)
                   .TotalMilliseconds.Should().BeApproximately(CycleConfiguration.Default.CycleDuration.TotalMilliseconds, 0.0001d,
                        "the provider should start on the second cycle");

                foreach (var phases in receivedPhases.Skip(PhaseCountPerCycle)
                   .Zip(receivedPhases2, (a, b) => new Tuple<IPhase, IPhase>(a, b)))
                {
                    (phases.Item1.UtcStartTime - phases.Item2.UtcStartTime).TotalMilliseconds
                       .Should().BeApproximately(0, 0.0001d, "phases should be in sync");
                    phases.Item1.Name.Should().Be(phases.Item2.Name);
                    phases.Item1.Status.Should().Be(phases.Item1.Status);
                }
            }
        }

        public void Dispose()
        {
            _cycleProvider?.Dispose();
            _subscription?.Dispose();
        }
    }
}
