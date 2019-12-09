using System;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class DnsApiTest
    {
        private IDfsService ipfs;

        public DnsApiTest(ITestOutputHelper output)
        {
            ipfs = TestDfs.GetTestDfs(output);  
        }

        [Fact]
        public async Task Resolve()
        {
            var path = await ipfs.DnsApi.ResolveAsync("ipfs.io");
            Assert.NotNull(path);
        }

        [Fact]
        public void Resolve_NoLink()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ipfs.DnsApi.ResolveAsync("google.com").Result;
            });
        }

        [Fact]
        public async Task Resolve_Recursive()
        {
            var path = await ipfs.DnsApi.ResolveAsync("ipfs.io", true);
            Assert.StartsWith("/ipfs/", path);
        }
    }
}
