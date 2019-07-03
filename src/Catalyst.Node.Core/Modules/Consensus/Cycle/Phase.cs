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
using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Modules.Consensus.Cycle;
using Multiformats.Hash;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Catalyst.Node.Core.Modules.Consensus.Cycle
{
    /// <inheritdoc cref="IPhase"/>
    public class Phase : IPhase
    {
        public Phase(Multihash previousDeltaDfsHash, 
            PhaseName phaseName, 
            PhaseStatus phaseStatus,
            DateTime utcStartTime)
        {
            PreviousDeltaDfsHash = previousDeltaDfsHash;
            Name = phaseName;
            UtcStartTime = utcStartTime;
            Status = phaseStatus;
        }

        /// <inheritdoc />
        public Multihash PreviousDeltaDfsHash { get; }

        /// <inheritdoc />
        public PhaseName Name { get; }

        /// <inheritdoc />
        public PhaseStatus Status { get; }

        /// <inheritdoc />
        public DateTime UtcStartTime { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name} : {Status} @ {UtcStartTime:O} | {PreviousDeltaDfsHash}";
        }
    }
}
