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

namespace Catalyst.Abstractions.Consensus.Cycle
{
    /// <summary>
    /// Use this service to get notification about the different events happening during the
    /// delta production cycles.
    /// </summary>
    public interface ICycleEventsProvider
    {
        /// <summary>
        /// Configuration object holding the duration of the different phases in the cycle.
        /// </summary>
        ICycleConfiguration Configuration { get; }

        /// <summary>
        /// Subscribe to this stream to get notified about cycle and phaseDetails changes
        /// </summary>
        IObservable<IPhase> PhaseChanges { get; }

        /// <summary>
        /// Use this method to find out in how much time the next production cycle will start.
        /// </summary>
        /// <returns>A TimeSpan representing the time to wait until next delta production cycle starts.</returns>
        TimeSpan GetTimeSpanUntilNextCycleStart();

        /// <summary>
        /// Terminate the emission of state changes events on the <see cref="PhaseChanges"/> stream
        /// </summary>
        void Close();
    }
}

