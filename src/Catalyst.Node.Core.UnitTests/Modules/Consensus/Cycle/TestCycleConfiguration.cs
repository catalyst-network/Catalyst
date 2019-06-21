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

using Catalyst.Common.Modules.Consensus.Cycle;
using Catalyst.Node.Core.Modules.Consensus.Cycle;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus.Cycle
{
    public class TestCycleConfiguration : CycleConfiguration
    {
        private const int CompressionFactor = 10;

        public static CycleConfiguration TestDefault = new TestCycleConfiguration(Default.Construction.CompressTime(CompressionFactor),
            Default.Construction.CompressTime(CompressionFactor),
            Default.Voting.CompressTime(CompressionFactor),
            Default.Synchronisation.CompressTime(CompressionFactor));

        protected TestCycleConfiguration(PhaseTimings construction,
            PhaseTimings campaigning,
            PhaseTimings voting,
            PhaseTimings synchronisation)
            : base(construction, campaigning, voting, synchronisation) { }
    }

    public static class PhaseTimingsExtensions
    {
        public static PhaseTimings CompressTime(this PhaseTimings originalTimings, double compressionFactor)
        {
            var compressedTimings = new PhaseTimings(originalTimings.Offset.Divide(compressionFactor),
                originalTimings.ProductionTime.Divide(compressionFactor),
                originalTimings.CollectionTime.Divide(compressionFactor));
            return compressedTimings;
        }
    }
}
