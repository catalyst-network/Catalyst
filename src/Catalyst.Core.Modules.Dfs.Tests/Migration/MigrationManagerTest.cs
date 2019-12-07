using System;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.Migration;
using Catalyst.Core.Modules.Dfs.Migration;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.Migration
{
    public class MigrationManagerTest
    {
        private IDfs ipfs;

        public MigrationManagerTest(ITestOutputHelper output)
        {
            ipfs = new TestFixture(output).Ipfs;      
        }
        
        [Fact]
        public void HasMigrations()
        {
            var migrator = new MigrationManager(ipfs);
            var migrations = migrator.Migrations;
            Assert.NotEqual(0, migrations.Count);
        }

        [Fact]
        public void MirgrateToUnknownVersion()
        {
            var migrator = new MigrationManager(ipfs);
            ExceptionAssert.Throws<ArgumentOutOfRangeException>(() =>
            {
                migrator.MirgrateToVersionAsync(int.MaxValue).Wait();
            });
        }

        [Fact]
        public async Task MigrateToLowestThenHighest()
        {
            using (var ipfs = new TempNode())
            {
                var migrator = new MigrationManager(ipfs);
                await migrator.MirgrateToVersionAsync(0);
                Assert.Equal(0, migrator.CurrentVersion);

                await migrator.MirgrateToVersionAsync(migrator.LatestVersion);
                Assert.Equal(migrator.LatestVersion, migrator.CurrentVersion);
            }
        }
    }
}
