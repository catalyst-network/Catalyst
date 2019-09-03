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
using Catalyst.Abstractions.Consensus.Cycle;

namespace Catalyst.Core.Consensus.Cycle
{
    /// <inheritdoc />
    public class CycleConfiguration : ICycleConfiguration
    {
        protected CycleConfiguration(PhaseTimings construction, 
            IPhaseTimings campaigning, 
            IPhaseTimings voting, 
            IPhaseTimings synchronisation)
        {
            CycleDuration = construction.TotalTime + campaigning.TotalTime + voting.TotalTime +
                synchronisation.TotalTime;
            TimingsByName = new Dictionary<IPhaseName, IPhaseTimings>
            {
                {PhaseName.Construction, construction},
                {PhaseName.Campaigning, campaigning},
                {PhaseName.Voting, voting},
                {PhaseName.Synchronisation, synchronisation}
            };
        }

        /// <inheritdoc />
        public IPhaseTimings Construction => TimingsByName[PhaseName.Construction];

        /// <inheritdoc />
        public IPhaseTimings Campaigning => TimingsByName[PhaseName.Campaigning];

        /// <inheritdoc />
        public IPhaseTimings Voting => TimingsByName[PhaseName.Voting];

        /// <inheritdoc />
        public IPhaseTimings Synchronisation => TimingsByName[PhaseName.Synchronisation];

        /// <inheritdoc />
        public TimeSpan CycleDuration { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<IPhaseName, IPhaseTimings> TimingsByName { get; }

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
