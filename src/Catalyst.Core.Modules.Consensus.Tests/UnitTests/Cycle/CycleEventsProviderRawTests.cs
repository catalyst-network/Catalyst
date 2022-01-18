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

using Catalyst.Abstractions.Consensus.Cycle;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Types;
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Modules.Consensus.Cycle;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Protocol.Deltas;
using FluentAssertions;
using Lib.P2P;
using Nethermind.Core;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.Cycle
{
    [TestFixture]
    public sealed class CycleEventsProviderRawTests
    {
        private ICycleEventsProvider _cycleEventsProvider;
        private ManualResetEventSlim _manualResetEventSlim;
        private IKeySigner _keySigner;
        private IValidatorSetStore _validatorSetStore;
        private IDeltaCache _deltaCache;

        [SetUp]
        public void Init()
        {
            FfiWrapper cryptoContext = new();
            var privateKey = cryptoContext.GeneratePrivateKey();
            var publicKey = privateKey.GetPublicKey();

            _keySigner = Substitute.For<IKeySigner>();
            _keySigner.GetPrivateKey(KeyRegistryTypes.DefaultKey).GetPublicKey().Returns(publicKey);

            _validatorSetStore = Substitute.For<IValidatorSetStore>();
            _validatorSetStore.Get(Arg.Any<long>()).GetValidators().Returns(new List<Address> { publicKey.ToKvmAddress() });

            var deltaNumber = 0;
            _deltaCache = Substitute.For<IDeltaCache>();
            _deltaCache.TryGetOrAddConfirmedDelta(Arg.Any<Cid>(), out Arg.Any<Delta>())
                .Returns(x =>
                {
                    x[1] = new Delta { DeltaNumber = deltaNumber++ };
                    return true;
                });

            _cycleEventsProvider = new CycleEventsProviderRaw(CycleConfiguration.Default, new DateTimeProvider(), Substitute.For<IDeltaHashProvider>(), _deltaCache, _validatorSetStore, _keySigner, Substitute.For<ILogger>());

            //Block until we receive a completed event from the PhaseChanges observable.
            _manualResetEventSlim = new ManualResetEventSlim();
        }

        [TearDown]
        public void TearDown()
        {
            _cycleEventsProvider.Dispose();
            _manualResetEventSlim.Dispose();
        }

        [Test]
        public async Task Should_Produce_Phases_In_Order()
        {
            //The cycle event phases in order.
            var phasesInOrder = new List<(PhaseName, PhaseStatus)> {
                (PhaseName.Construction, PhaseStatus.Producing),
                (PhaseName.Construction, PhaseStatus.Collecting),
                (PhaseName.Campaigning, PhaseStatus.Producing),
                (PhaseName.Campaigning, PhaseStatus.Collecting),
                (PhaseName.Voting, PhaseStatus.Producing),
                (PhaseName.Voting, PhaseStatus.Collecting),
                (PhaseName.Synchronisation, PhaseStatus.Producing),
                (PhaseName.Synchronisation, PhaseStatus.Collecting)
            };

            //Start the event cycle.
            await _cycleEventsProvider.StartAsync();

            var fakeObserver = Substitute.For<IObserver<IPhase>>();

            //Listen for new phase events.
            var phaseChangesDisposable = _cycleEventsProvider.PhaseChanges.Subscribe(fakeObserver);

            Task.Delay(_cycleEventsProvider.Configuration.CycleDuration * 2).Wait();

            //Check the phase events are in the received and in correct order
            Received.InOrder(() =>
            {
                var phases = fakeObserver.ReceivedCalls().Select(c => c.GetArguments()[0]).Cast<IPhase>().ToList();
                for (var i = 0; i < phases.Count(); i++)
                {
                    var phaseOrderIndex = i % phasesInOrder.Count();
                    phases[i].Name.Should().Be(phasesInOrder[phaseOrderIndex].Item1);
                    phases[i].Status.Should().Be(phasesInOrder[phaseOrderIndex].Item2);
                }
            });

            phaseChangesDisposable.Dispose();
        }

        [Test]
        public async Task StartAsync_Should_Start_CycleEventsProvider()
        {
            //Should be populated to a construction phase with a producing status.
            IPhase phase = null;

            //Listen for new phase events.
            var phaseChangesDisposable = _cycleEventsProvider.PhaseChanges.Subscribe(currentPhase =>
            {
                phase = currentPhase;
                _manualResetEventSlim.Set();
            });

            //Start the event cycle.
            await _cycleEventsProvider.StartAsync();


            //Wait for PhaseChanges to complete else fail if it times out.
            if (!_manualResetEventSlim.Wait(CycleConfiguration.Default.CycleDuration))
            {
                Assert.Fail();
            }

            //PhaseStatus should be a construction phase with a producing status.
            phase.Name.Should().Be(PhaseName.Construction);
            phase.Status.Should().Be(PhaseStatus.Producing);

            phaseChangesDisposable.Dispose();
        }

        [Test]
        public async Task Close_Should_Stop_CycleEventsProvider()
        {
            //Listen for completed event
            var phaseChangesDisposable = _cycleEventsProvider.PhaseChanges.Subscribe(currentPhase => { }, () =>
            {
                _manualResetEventSlim.Set();
            });

            //Start the event cycle.
            await _cycleEventsProvider.StartAsync();

            //Stop the event cycle provider.
            _cycleEventsProvider.Close();

            //Wait for PhaseChanges to complete else fail if it times out.
            if (!_manualResetEventSlim.Wait(CycleConfiguration.Default.CycleDuration))
            {
                Assert.Fail();
            }

            phaseChangesDisposable.Dispose();
        }

        [Test]
        public void PhaseChanges_Should_Be_Synchronised_Across_Instances()
        {
            //Offset to start the second event cycle provider at.
            var secondProviderStartOffset = CycleConfiguration.Default.CycleDuration.Divide(3);

            //Create a second event cycle provider
            using CycleEventsProviderRaw cycleEventsProvider2 = new(CycleConfiguration.Default, new DateTimeProvider(), Substitute.For<IDeltaHashProvider>(), _deltaCache, _validatorSetStore, _keySigner, Substitute.For<ILogger>());

            //Create event observers for the cycle event providers
            var fakeObserver1 = Substitute.For<IObserver<IPhase>>();
            var fakeObserver2 = Substitute.For<IObserver<IPhase>>();

            //Listen for new phase events.
            var phaseChangesDisposable1 = _cycleEventsProvider.PhaseChanges.Subscribe(fakeObserver1);
            var phaseChangesDisposable2 = cycleEventsProvider2.PhaseChanges.Subscribe(fakeObserver2);

            //Start first event provider.
            _cycleEventsProvider.StartAsync();

            //Delay the second event provider.
            Task.Delay(secondProviderStartOffset).Wait();

            //Start second event provider.
            cycleEventsProvider2.StartAsync();

            //Order all the received phases and only compare the same phases.
            Received.InOrder(() =>
            {
                var cycleEventsProviderPhases1 = fakeObserver1.ReceivedCalls().Select(c => c.GetArguments()[0]).Cast<IPhase>().ToList();
                var cycleEventsProviderPhases2 = fakeObserver2.ReceivedCalls().Select(c => c.GetArguments()[0]).Cast<IPhase>().ToList();

                var callsOffset = cycleEventsProviderPhases1.Count() - cycleEventsProviderPhases2.Count();

                for (var i = 0; i < cycleEventsProviderPhases2.Count(); i++)
                {
                    var phase1 = cycleEventsProviderPhases1[i + callsOffset];
                    var phase2 = cycleEventsProviderPhases2[i];

                    phase1.Name.Should().Be(phase2.Name);
                    phase1.Status.Should().Be(phase2.Status);
                    (phase1.UtcStartTime - phase2.UtcStartTime).TotalMilliseconds.Should().BeApproximately(0, 0.0001d, "phases should be in sync");
                }
            });

            phaseChangesDisposable1.Dispose();
            phaseChangesDisposable2.Dispose();
        }
    }
}
