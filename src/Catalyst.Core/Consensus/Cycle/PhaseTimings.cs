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
using Catalyst.Abstractions.Consensus.Cycle;

namespace Catalyst.Core.Consensus.Cycle
{
    /// <summary>
    /// A placeholder for the timespans corresponding to the different periods of a given phase.
    /// Each phase starts with an Offset, then 
    /// </summary>
    public class PhaseTimings : IPhaseTimings
    {
        public PhaseTimings(TimeSpan offset, TimeSpan productionTime, TimeSpan collectionTime)
        {
            Offset = offset;
            ProductionTime = productionTime;
            CollectionTime = collectionTime;
        }

        /// <summary>
        /// Time between the start of a cycle and the start of the phase.
        /// </summary>
        public TimeSpan Offset { get; }

        /// <summary>
        /// Time during which a phase is in the <see cref="PhaseStatus.Producing"/> status.
        /// </summary>
        public TimeSpan ProductionTime { get; }

        /// <summary>
        /// Time during which a phase is in the <see cref="PhaseStatus.Collecting"/> status.
        /// </summary>
        public TimeSpan CollectionTime { get; }

        /// <summary>
        /// The total duration of the phase, after which it will go <see cref="PhaseStatus.Idle"/>
        /// until the next cycle.
        /// </summary>
        public TimeSpan TotalTime => ProductionTime + CollectionTime;
    }
}
