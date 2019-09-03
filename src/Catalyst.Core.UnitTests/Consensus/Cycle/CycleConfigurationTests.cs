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

using Catalyst.Core.Consensus.Cycle;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.Consensus.Cycle
{
    public class CycleConfigurationTests
    {
        [Fact]
        public void Default_CycleConfiguration_should_not_have_overlapping_phases()
        {
            var config = CycleConfiguration.Default;

            config.Campaigning.Offset.Should().Be(config.Construction.Offset + config.Construction.TotalTime);
            config.Voting.Offset.Should().Be(config.Campaigning.Offset + config.Campaigning.TotalTime);
            config.Synchronisation.Offset.Should().Be(config.Voting.Offset + config.Voting.TotalTime);
            config.CycleDuration.Should().Be(config.Synchronisation.Offset + config.Synchronisation.TotalTime);
        }
    }
}
