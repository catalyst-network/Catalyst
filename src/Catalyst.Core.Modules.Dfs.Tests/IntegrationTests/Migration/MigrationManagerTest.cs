using System;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Modules.Dfs.Migration;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.Migration
{
    public class MigrationManagerTest
    {
        private IDfsService ipfs;
        private ITestOutputHelper _testOutput;

        public MigrationManagerTest(ITestOutputHelper output)
        {
            _testOutput = output;
            ipfs = TestDfs.GetTestDfs(output);      
        }
        
        [Fact]
        public void HasMigrations()
        {
            var migrator = new MigrationManager(ipfs.Options.Repository);
            var migrations = migrator.Migrations;
            Assert.NotEqual(0, migrations.Count);
        }

        [Fact]
        public void MirgrateToUnknownVersion()
        {
            var migrator = new MigrationManager(ipfs.Options.Repository);
            ExceptionAssert.Throws<ArgumentOutOfRangeException>(() =>
            {
                migrator.MirgrateToVersionAsync(int.MaxValue).Wait();
            });
        }

        [Fact]
        public async Task MigrateToLowestThenHighest()
        {
            using (var ipfs = TestDfs.GetTestDfs(_testOutput))
            {
                var migrator = new MigrationManager(ipfs.Options.Repository);
                await migrator.MirgrateToVersionAsync(0);
                Assert.Equal(0, migrator.CurrentVersion);

                await migrator.MirgrateToVersionAsync(migrator.LatestVersion);
                Assert.Equal(migrator.LatestVersion, migrator.CurrentVersion);
            }
        }
    }
}
