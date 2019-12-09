using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class StatsApiTest
    {
        private IDfsService ipfs;

        public StatsApiTest(ITestOutputHelper output)
        {
            ipfs = TestDfs.GetTestDfs(output);
        }
        
        [Fact]
        public void Exists() { Assert.NotNull(ipfs.StatsApi); }

        [Fact]
        public async Task SmokeTest()
        {
            var bandwidth = await ipfs.StatsApi.BandwidthAsync();
            var bitswap = await ipfs.StatsApi.BitSwapAsync();
            var repository = await ipfs.StatsApi.RepositoryAsync();
        }
    }
}
