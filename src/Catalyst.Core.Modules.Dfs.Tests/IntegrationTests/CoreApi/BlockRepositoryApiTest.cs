using System.IO;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public sealed class BlockRepositoryApiTest
    {
        private IDfsService ipfs;

        public BlockRepositoryApiTest(ITestOutputHelper output)
        {
            ipfs = TestDfs.GetTestDfs(output);    
        }
        
        [Fact]
        public void Exists() { Assert.NotNull(ipfs.BlockRepositoryApi); }

        [Fact]
        public async Task Stats()
        {
            var stats = await ipfs.BlockRepositoryApi.StatisticsAsync();
            var version = await ipfs.BlockRepositoryApi.VersionAsync();
            Assert.Equal(stats.Version, version);
        }

        [Fact]
        public async Task GarbageCollection()
        {
            var pinned = await ipfs.BlockApi.PutAsync(new byte[256], pin: true);
            var unpinned = await ipfs.BlockApi.PutAsync(new byte[512], pin: false);
            Assert.NotEqual(pinned, unpinned);
            Assert.NotNull(await ipfs.BlockApi.StatAsync(pinned));
            Assert.NotNull(await ipfs.BlockApi.StatAsync(unpinned));

            await ipfs.BlockRepositoryApi.RemoveGarbageAsync();
            Assert.NotNull(await ipfs.BlockApi.StatAsync(pinned));
            Assert.Null(await ipfs.BlockApi.StatAsync(unpinned));
        }

        [Fact]
        public async Task Version_Info()
        {
            var versions = await ipfs.BlockRepositoryApi.VersionAsync();
            Assert.NotNull(versions);
         
            // Assert.True(versions.ContainsKey("Version"));
            // Assert.True(versions.ContainsKey("Repo"));
        }
        
        [Fact]
        public async Task VersionFileMissing()
        {
            var versionPath = Path.Combine(ipfs.Options.Repository.ExistingFolder(), "version");
            var versionBackupPath = versionPath + ".bak";

            try
            {
                if (File.Exists(versionPath))
                {
                    File.Move(versionPath, versionBackupPath);
                }

                Assert.Equal("0", await ipfs.BlockRepositoryApi.VersionAsync());
            }
            finally
            {
                if (File.Exists(versionBackupPath))
                {
                    File.Move(versionBackupPath, versionPath);
                }
            }
        }
    }
}
