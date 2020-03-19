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

using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public sealed class StatsApiTest
    {
        private readonly IDfsService _dfs;

        public StatsApiTest(TestContext output)
        {
            _dfs = TestDfs.GetTestDfs(output);
        }
        
        [Test]
        public void Exists() { Assert.NotNull(_dfs.StatsApi); }

        [Test]
        public async Task SmokeTest()
        {
            await _dfs.StatsApi.GetBandwidthStatsAsync();
            _dfs.StatsApi.GetBitSwapStats();
            await _dfs.StatsApi.GetRepositoryStatsAsync();
        }
    }
}
