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

using FluentAssertions;
using Xunit;

namespace Catalyst.Core.Modules.Kvm.Tests.UnitTests
{
    /// <summary>
    /// This is just a placeholder for the actual tests later.
    /// </summary>
    public sealed class RangeProofPrecompileTests
    {
        [Fact]
        public void Base_Gas_Cost_Should_Return_200000()
        {
            var precompile = new RangeProofPrecompile();
            var baseCost = precompile.BaseGasCost(CatalystGenesisSpec.Instance);
            baseCost.Should().Be(200000);
        }

        [Fact]
        public void Base_Data_Gas_Cost_Should_Return_0()
        {
            var precompile = new RangeProofPrecompile();
            var dataCost = precompile.DataGasCost(new byte[32], CatalystGenesisSpec.Instance);
            dataCost.Should().Be(0);
        }
    }
}
