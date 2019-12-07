using System;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class NameApiTest
    {
        private IDfs ipfs;

        public NameApiTest(ITestOutputHelper output)
        {
            ipfs = new TestFixture(output).Ipfs;      
        }

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
            Assert.StartsWith("/ipfs/", path);
            Assert.EndsWith("/media", path);

            path = await ipfs.Name.ResolveAsync("ipfs.io/media", true);
            Assert.StartsWith("/ipfs/", path);
            Assert.EndsWith("/media", path);

            path = await ipfs.Name.ResolveAsync("/ipfs.io/media", true);
            Assert.StartsWith("/ipfs/", path);
            Assert.EndsWith("/media", path);
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
