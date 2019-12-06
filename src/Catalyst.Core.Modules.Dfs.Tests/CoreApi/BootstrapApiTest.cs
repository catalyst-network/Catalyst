using System.Linq;
using System.Threading.Tasks;
using MultiFormats;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class BootstapApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;
        MultiAddress somewhere = "/ip4/127.0.0.1/tcp/4009/ipfs/QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";

        [Fact]
        public async Task Add_Remove()
        {
            var addr = await ipfs.Bootstrap.AddAsync(somewhere);
            Assert.NotNull(addr);
            Assert.Equal(somewhere, addr);
            var addrs = await ipfs.Bootstrap.ListAsync();
            Assert.True(addrs.Any(a => a == somewhere));

            addr = await ipfs.Bootstrap.RemoveAsync(somewhere);
            Assert.NotNull(addr);
            Assert.Equal(somewhere, addr);
            addrs = await ipfs.Bootstrap.ListAsync();
            Assert.False(addrs.Any(a => a == somewhere));
        }

        [Fact]
        public async Task List()
        {
            var addrs = await ipfs.Bootstrap.ListAsync();
            Assert.NotNull(addrs);
            Assert.NotEqual(0, addrs.Count());
        }

        [Fact]
        public async Task Remove_All()
        {
            var original = await ipfs.Bootstrap.ListAsync();
            await ipfs.Bootstrap.RemoveAllAsync();
            var addrs = await ipfs.Bootstrap.ListAsync();
            Assert.Equal(0, addrs.Count());
            foreach (var addr in original)
            {
                await ipfs.Bootstrap.AddAsync(addr);
            }
        }

        [Fact]
        public async Task Add_Defaults()
        {
            var original = await ipfs.Bootstrap.ListAsync();
            await ipfs.Bootstrap.RemoveAllAsync();
            try
            {
                await ipfs.Bootstrap.AddDefaultsAsync();
                var addrs = await ipfs.Bootstrap.ListAsync();
                Assert.NotEqual(0, addrs.Count());
            }
            finally
            {
                await ipfs.Bootstrap.RemoveAllAsync();
                foreach (var addr in original)
                {
                    await ipfs.Bootstrap.AddAsync(addr);
                }
            }
        }

        [Fact]
        public async Task Override_FactoryDefaults()
        {
            var original = ipfs.Options.Discovery.BootstrapPeers;
            try
            {
                ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
                var addrs = await ipfs.Bootstrap.ListAsync();
                Assert.Equal(0, addrs.Count());

                ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[1]
                    {somewhere};
                addrs = await ipfs.Bootstrap.ListAsync();
                Assert.Equal(1, addrs.Count());
                Assert.Equal(somewhere, addrs.First());
            }
            finally
            {
                ipfs.Options.Discovery.BootstrapPeers = original;
            }
        }
    }
}
