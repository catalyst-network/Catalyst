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
using Multiformats.Hash;

namespace Catalyst.Abstractions.Consensus.Cycle
{
    /// <summary>
    /// Represents a given phaseDetails of the ledger update cycle, namely,
    /// Construction, Campaigning, Voting and Synchronisation.
    /// </summary>
    public interface IPhase
    {
        /// <summary>
        /// Address on the DFS of the delta elected on the previous cycle, here used as
        /// a unique identifier for this phase.
        /// </summary>
        Multihash PreviousDeltaDfsHash { get; }

        /// <summary>
        /// The name of the phase represented by this instance.
        /// </summary>
        IPhaseName Name { get; }

        /// <summary>
        /// Status in which our Phase is.
        /// </summary>
        IPhaseStatus Status { get; }

        /// <summary>
        /// The time at which the phase was started.
        /// </summary>
        DateTime UtcStartTime { get; }
    }
}
