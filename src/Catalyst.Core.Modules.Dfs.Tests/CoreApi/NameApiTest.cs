using System;
using System.Threading.Tasks;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    [TestClass]
    public class NameApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;

        [Fact]
        public async Task Resolve_DnsLink()
        {
            var iopath = await ipfs.Name.ResolveAsync("ipfs.io");
            Assert.NotNull(iopath);

            var path = await ipfs.Name.ResolveAsync("/ipns/ipfs.io");
            Assert.Equal(iopath, path);
        }

        [Fact]
        public async Task Resolve_DnsLink_Recursive()
        {
            var path = await ipfs.Name.ResolveAsync("/ipns/ipfs.io/media", true);
            Assert.StartsWith(path, "/ipfs/");
            Assert.EndsWith(path, "/media");

            path = await ipfs.Name.ResolveAsync("ipfs.io/media", true);
            Assert.StartsWith(path, "/ipfs/");
            Assert.EndsWith(path, "/media");

            path = await ipfs.Name.ResolveAsync("/ipfs.io/media", true);
            Assert.StartsWith(path, "/ipfs/");
            Assert.EndsWith(path, "/media");
        }

        [Fact]
        public void Resolve_NoDnsLink()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ipfs.Dns.ResolveAsync("google.com").Result;
            });
        }
    }
}
