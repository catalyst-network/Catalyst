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
    public class PhaseName : Enumeration, IPhaseName
    {
        /// <summary>
        /// During this phase, delta producers are building their own version of the delta
        /// </summary>
        public static readonly PhaseName Construction = new ConstructionPhaseName();

        /// <summary>
        /// During this phase, delta producers are promoting their own version of the delta
        /// and receiving versions from other producers
        /// </summary>
        public static readonly PhaseName Campaigning = new CampaigningPhaseName();

        /// <summary>
        /// During this phase, delta producers are voting for what they think is the most
        /// popular candidate delta amongst deltas received in the campaigning phase.
        /// </summary>
        public static readonly PhaseName Voting = new VotingPhaseName();

        /// <summary>
        /// During this phase, delta producers are broadcasting and receiving the elected
        /// candidate delta, and ensuring it makes its way onto the DFS.
        /// </summary>
        public static readonly PhaseName Synchronisation = new SynchronisationPhaseName();
     
        private PhaseName(int id, string name) 
            : base(id, name) { }

        private sealed class ConstructionPhaseName : PhaseName
        {
            public ConstructionPhaseName() : base(1, "Construction") { }
        }

        private sealed class CampaigningPhaseName : PhaseName
        {
            public CampaigningPhaseName() : base(2, "Campaigning") { }
        }

        private sealed class VotingPhaseName : PhaseName
        {
            public VotingPhaseName() : base(3, "Voting") { }
        }

        private sealed class SynchronisationPhaseName : PhaseName
        {
            public SynchronisationPhaseName() : base(4, "Synchronisation") { }
        }
    }
}
