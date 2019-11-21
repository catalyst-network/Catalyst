using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Core.Tests.CoreApi
{
    [TestClass]
    public class StatsApiTest
    {
        IpfsEngine _ipfs = TestFixture.Ipfs;

        [TestMethod]
        public void Exists() { Assert.IsNotNull(_ipfs.Stats); }

        [TestMethod]
        public async Task SmokeTest()
        {
            var bandwidth = await _ipfs.Stats.BandwidthAsync();
            var bitswap = await _ipfs.Stats.BitswapAsync();
            var repository = await _ipfs.Stats.RepositoryAsync();
        }
    }
}
