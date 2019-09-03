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
using Catalyst.Abstractions.Enumerator;

namespace Catalyst.Core.Consensus.Cycle
{
    public class PhaseStatus : Enumeration, IPhaseStatus
    {
        /// <summary>
        /// During this part of the phase, delta producers are able to be active,
        /// producing a delta, voting for candidates, selecting what they think is the most popular
        /// candidate delta, or broadcasting an elected delta onto the network.
        /// </summary>
        public static readonly PhaseStatus Producing = new ProducingStatus();

        /// <summary>
        /// A production period is followed by a collection period, a sort of cooling off
        /// period during which only the collection of deltas or votes is allowed.
        /// </summary>
        public static readonly PhaseStatus Collecting = new CollectingStatus();

        /// <summary>
        /// A phase is Idle when it is not meant to be doing anything, i.e. when another
        /// phase is either in its <see cref="Producing"/> or <see cref="Collecting"/> mode.
        /// </summary>
        public static readonly PhaseStatus Idle = new IdleStatus();

        private PhaseStatus(int id, string name) : base(id, name) { }

        public sealed class ProducingStatus : PhaseStatus
        {
            public ProducingStatus() : base(1, "Producing") { }
        }

        public sealed class CollectingStatus : PhaseStatus
        {
            public CollectingStatus() : base(2, "Collecting") { }
        }

        public sealed class IdleStatus : PhaseStatus
        {
            public IdleStatus() : base(3, "Idle") { }
        }
    }
}
