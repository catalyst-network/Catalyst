using System.IO;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class BlockRepositoryApiTest
    {
        private IDfs ipfs;

        public BlockRepositoryApiTest(ITestOutputHelper output)
        {
            ipfs = new TestFixture(output).Ipfs;    
        }
        
        [Fact]
        public void Exists() { Assert.NotNull(ipfs.BlockRepository); }

        [Fact]
        public async Task Stats()
        {
            var stats = await ipfs.BlockRepository.StatisticsAsync();
            var version = await ipfs.BlockRepository.VersionAsync();
            Assert.Equal(stats.Version, version);
        }

        [Fact]
        public async Task GarbageCollection()
        {
            var pinned = await ipfs.Block.PutAsync(new byte[256], pin: true);
            var unpinned = await ipfs.Block.PutAsync(new byte[512], pin: false);
            Assert.NotEqual(pinned, unpinned);
            Assert.NotNull(await ipfs.Block.StatAsync(pinned));
            Assert.NotNull(await ipfs.Block.StatAsync(unpinned));

            await ipfs.BlockRepository.RemoveGarbageAsync();
            Assert.NotNull(await ipfs.Block.StatAsync(pinned));
            Assert.Null(await ipfs.Block.StatAsync(unpinned));
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

                Assert.Equal("0", await ipfs.BlockRepository.VersionAsync());
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
