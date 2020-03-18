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
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.Migration;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.Migration
{
    public sealed class MigrationManagerTest
    {
        private readonly IDfsService _dfs;

        public MigrationManagerTest(ITestOutputHelper output)
        {
            _dfs = TestDfs.GetTestDfs(output);      
        }
        
        [Fact]
        public void HasMigrations()
        {
            var migrator = new MigrationManager(_dfs.Options.Repository);
            var migrations = migrator.Migrations;
            Assert.NotEqual(0, migrations.Count);
        }

        [Fact]
        public void MirgrateToUnknownVersion()
        {
            var migrator = new MigrationManager(_dfs.Options.Repository);
            ExceptionAssert.Throws<ArgumentOutOfRangeException>(() =>
            {
                migrator.MirgrateToVersionAsync(int.MaxValue).Wait();
            });
        }

        [Fact]
        public async Task MigrateToLowestThenHighest()
        {
            var migrator = new MigrationManager(_dfs.Options.Repository);
            await migrator.MirgrateToVersionAsync(0);
            Assert.Equal(0, migrator.CurrentVersion);

            await migrator.MirgrateToVersionAsync(migrator.LatestVersion);
            Assert.Equal(migrator.LatestVersion, migrator.CurrentVersion);
        }
    }
}
