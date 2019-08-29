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

namespace Catalyst.Abstractions.Consensus.Cycle
{
    /// <summary>
    /// This can be thought of as a sort of configuration used by the <see cref="ICycleEventsProvider"/>
    /// to set the durations of the different phases in a cycle. A given cycle has 4 distinct phases
    /// occuring in the following order: <see cref="Construction"/>, <see cref="Campaigning"/>,
    /// <see cref="Voting"/> and <see cref="Synchronisation"/>.
    /// </summary>
    public interface ICycleConfiguration
    {
        /// <inheritdoc cref="PhaseName.Construction"/>>
        IPhaseTimings Construction { get; }

        /// <inheritdoc cref="PhaseName.Campaigning"/>>
        IPhaseTimings Campaigning { get; }

        /// <inheritdoc cref="PhaseName.Voting"/>>
        IPhaseTimings Voting { get; }

        /// <inheritdoc cref="PhaseName.Synchronisation"/>>
        IPhaseTimings Synchronisation { get; }

        /// <summary>
        /// The total duration of a cycle composed of the 4 phases <see cref="Construction"/>,
        /// <see cref="Campaigning"/>, <see cref="Voting"/> and <see cref="Synchronisation"/>
        /// </summary>
        TimeSpan CycleDuration { get; }

        /// <summary>
        /// This dictionary can be used to retrieve the <see cref="PhaseTimings"/> for a
        /// give <see cref="PhaseName"/>
        /// </summary>
        IReadOnlyDictionary<IPhaseName, IPhaseTimings> TimingsByName { get; }
    }
}
