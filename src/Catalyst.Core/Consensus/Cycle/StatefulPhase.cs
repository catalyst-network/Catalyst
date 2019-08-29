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
using Catalyst.Abstractions.Consensus.Cycle;

namespace Catalyst.Core.Consensus.Cycle
{
    public class StatefulPhase
    {
        public StatefulPhase(IPhaseName name, IPhaseStatus status)
        {
            Name = name;
            Status = status;
        }

        public IPhaseName Name { get; }
        public IPhaseStatus Status { get; }

        /// <summary>
        /// Use this to get a stream of state change events occuring at the time configured by <see cref="timings"/> every <see cref="cycleDuration"/>
        /// </summary>
        /// <param name="name">The name of the phase for which you need the state change events.</param>
        /// <param name="timings">The timing configuration for the phase.</param>
        /// <param name="cycleDuration">The total duration of a delta production cycle.</param>
        /// <param name="scheduler">The IScheduler used to synchronise the Timers.</param>
        /// <returns></returns>
        public static IObservable<StatefulPhase> GetStatusChangeObservable(IPhaseName name, 
            IPhaseTimings timings, 
            TimeSpan cycleDuration, 
            IScheduler scheduler)
        {
            var phaseInProducingStatus = Observable
               .Timer(timings.Offset, cycleDuration, scheduler)
               .Select(_ => new StatefulPhase(name, PhaseStatus.Producing));

            var phaseInCollectingStatus = Observable
               .Timer(timings.Offset + timings.ProductionTime, cycleDuration, scheduler)
               .Select(_ => new StatefulPhase(name, PhaseStatus.Collecting));

            var phaseInIdleStatus = Observable
               .Timer(timings.Offset + timings.TotalTime, cycleDuration, scheduler)
               .Select(_ => new StatefulPhase(name, PhaseStatus.Idle));

            return phaseInProducingStatus.Merge(phaseInCollectingStatus, scheduler).Merge(phaseInIdleStatus, scheduler);
        }
    }
}
