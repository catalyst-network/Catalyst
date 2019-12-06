using System;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class DnsApiTest
    {
        IDfs ipfs = TestFixture.Ipfs;

        [Fact]
        public async Task Resolve()
        {
            var path = await ipfs.Dns.ResolveAsync("ipfs.io");
            Assert.NotNull(path);
        }

        [Fact]
        public void Resolve_NoLink()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ipfs.Dns.ResolveAsync("google.com").Result;
            });
        }

        [Fact]
        public async Task Resolve_Recursive()
        {
            var path = await ipfs.Dns.ResolveAsync("ipfs.io", true);
            Assert.StartsWith(path, "/ipfs/");
        }
    }
}
