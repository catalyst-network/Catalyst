using System;
using System.IO;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class NameApiTest
    {
        private IDfsService ipfs;

        public NameApiTest(ITestOutputHelper output)
        {
            ipfs = TestDfs.GetTestDfs(output);
        }
        
        [Fact]
        public async Task Resolve_Cid()
        {
            var actual = await ipfs.NameApi.ResolveAsync("QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao");
            Assert.Equal("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao", actual);

            actual = await ipfs.NameApi.ResolveAsync("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao");
            Assert.Equal("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao", actual);
        }

        [Fact]
        public async Task Resolve_Cid_Path()
        {
            var temp = FileSystemApiTest.MakeTemp();
            try
            {
                var dir = await ipfs.UnixFsApi.AddDirectoryAsync(temp, true);
                var name = "/ipfs/" + dir.Id.Encode() + "/x/y/y.txt";
                Assert.Equal("/ipfs/QmTwEE2eSyzcvUctxP2negypGDtj7DQDKVy8s3Rvp6y6Pc",
                    await ipfs.NameApi.ResolveAsync(name));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }
        
        [Fact]
        public void Resolve_Cid_Invalid()
        {
            ExceptionAssert.Throws<FormatException>(() =>
            {
                var _ = ipfs.NameApi.ResolveAsync("QmHash").Result;
            });
        }

        [Fact]
        public async Task Resolve_DnsLink()
        {
            var iopath = await ipfs.NameApi.ResolveAsync("ipfs.io");
            Assert.NotNull(iopath);

            var path = await ipfs.NameApi.ResolveAsync("/ipns/ipfs.io");
            Assert.Equal(iopath, path);
        }

        [Fact]
        public async Task Resolve_DnsLink_Recursive()
        {
            var path = await ipfs.NameApi.ResolveAsync("/ipns/ipfs.io/media", true);
            Assert.StartsWith("/ipfs/", path);
            Assert.EndsWith("/media", path);

            path = await ipfs.NameApi.ResolveAsync("ipfs.io/media", true);
            Assert.StartsWith("/ipfs/", path);
            Assert.EndsWith("/media", path);

            path = await ipfs.NameApi.ResolveAsync("/ipfs.io/media", true);
            Assert.StartsWith("/ipfs/", path);
            Assert.EndsWith("/media", path);
        }

        [Fact]
        public void Resolve_NoDnsLink()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ipfs.DnsApi.ResolveAsync("google.com").Result;
            });
        }
        
        // [Fact]
        // [Ignore("Need a working IPNS")]
        // public async Task Resolve_DnsLink_Recursive()
        // {
        //     var ipfs = TestFixture.Ipfs;
        //
        //     var media = await ipfs.Generic.ResolveAsync("/ipns/ipfs.io/media");
        //     var actual = await ipfs.Generic.ResolveAsync("/ipns/ipfs.io/media", recursive: true);
        //     Assert.NotEqual(media, actual);
        // }
    }
}
