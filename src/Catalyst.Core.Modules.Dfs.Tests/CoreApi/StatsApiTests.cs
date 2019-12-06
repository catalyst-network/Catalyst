using System.Threading.Tasks;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    [TestClass]
    public class StatsApiTest
    {
        Dfs ipfs = TestFixture.Ipfs;

        [Fact]
        public void Exists() { Assert.NotNull(ipfs.Stats); }

        [Fact]
        public async Task SmokeTest()
        {
            var bandwidth = await ipfs.Stats.BandwidthAsync();
            var bitswap = await ipfs.Stats.BitswapAsync();
            var repository = await ipfs.Stats.RepositoryAsync();
        }
    }
}
