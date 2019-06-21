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

namespace Catalyst.Node.Core.Modules.Consensus.Cycle
{
    /// <inheritdoc />
    public class CycleConfiguration : ICycleConfiguration
    {
        protected CycleConfiguration(PhaseTimings construction, 
            PhaseTimings campaigning, 
            PhaseTimings voting, 
            PhaseTimings synchronisation)
        {
            Construction = construction;
            Campaigning = campaigning;
            Voting = voting;
            Synchronisation = synchronisation;
            CycleDuration = construction.TotalTime + campaigning.TotalTime + voting.TotalTime +
                synchronisation.TotalTime;
        }

        /// <inheritdoc />
        public PhaseTimings Construction { get; }

        /// <inheritdoc />
        public PhaseTimings Campaigning { get; }

        /// <inheritdoc />
        public PhaseTimings Voting { get; }

        /// <inheritdoc />
        public PhaseTimings Synchronisation { get; }

        /// <inheritdoc />
        public TimeSpan CycleDuration { get; }

        private static readonly TimeSpan ConstructionOffset = TimeSpan.Zero;
        private static readonly TimeSpan ConstructionProduction = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan ConstructionCollection = TimeSpan.FromSeconds(2);

        private static readonly PhaseTimings DefaultConstructionTimings = 
            new PhaseTimings(ConstructionOffset, ConstructionProduction, ConstructionCollection);

        private static readonly TimeSpan CampaigningOffset = ConstructionOffset + ConstructionProduction + ConstructionCollection;
        private static readonly TimeSpan CampaigningProduction = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan CampaigningCollection = TimeSpan.FromSeconds(3);

        private static readonly PhaseTimings DefaultCampaigningTimings = 
            new PhaseTimings(CampaigningOffset, CampaigningProduction, CampaigningCollection);

        private static readonly TimeSpan VotingOffset = CampaigningOffset + CampaigningProduction + CampaigningCollection;
        private static readonly TimeSpan VotingProduction = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan VotingCollection = TimeSpan.FromSeconds(2);

        private static readonly PhaseTimings DefaultVotingTimings = 
            new PhaseTimings(VotingOffset, VotingProduction, VotingCollection);

        private static readonly TimeSpan SynchronisationOffset = VotingOffset + VotingProduction + VotingCollection;
        private static readonly TimeSpan SynchronisationProduction = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan SynchronisationCollection = TimeSpan.FromSeconds(3);

        private static readonly PhaseTimings DefaultSynchronisationTimings = 
            new PhaseTimings(SynchronisationOffset, SynchronisationProduction, SynchronisationCollection);

        public static readonly CycleConfiguration Default = new CycleConfiguration(DefaultConstructionTimings, DefaultCampaigningTimings, DefaultVotingTimings, DefaultSynchronisationTimings);
    }
}
