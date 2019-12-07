using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class StatsApiTest
    {
        private IDfs ipfs;

        public StatsApiTest(ITestOutputHelper output)
        {
            ipfs = new TestFixture(output).Ipfs;      
        }
        
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
