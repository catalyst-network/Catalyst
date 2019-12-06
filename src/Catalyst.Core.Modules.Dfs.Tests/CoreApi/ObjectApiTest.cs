using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class ObjectApiTest
    {
        IDfs ipfs = TestFixture.Ipfs;

        [Fact]
        public async Task New_Template_Null()
        {
            var node = await ipfs.Object.NewAsync();
            Assert.Equal("QmdfTbBqBPQ7VNxZEYEj14VmRuZBkqFbiwReogJgS1zR1n", (string) node.Id);
        }

        [Fact]
        public async Task New_Template_UnixfsDir()
        {
            var node = await ipfs.Object.NewAsync("unixfs-dir");
            Assert.Equal("QmUNLLsPACCz1vLxQVkXqqLX5R1X345qqfHbsf67hvA3Nn", (string) node.Id);

            node = await ipfs.Object.NewDirectoryAsync();
            Assert.Equal("QmUNLLsPACCz1vLxQVkXqqLX5R1X345qqfHbsf67hvA3Nn", (string) node.Id);
        }

        [Fact]
        public void New_Template_Unknown()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var node = ipfs.Object.NewAsync("unknown-template").Result;
            });
        }

        [Fact]
        public async Task Put_Get_Dag()
        {
            var adata = Encoding.UTF8.GetBytes("alpha");
            var bdata = Encoding.UTF8.GetBytes("beta");
            var alpha = new DagNode(adata);
            var beta = new DagNode(bdata, new[] {alpha.ToLink()});
            var x = await ipfs.Object.PutAsync(beta);
            var node = await ipfs.Object.GetAsync(x.Id);
            Assert.Equal(beta.DataBytes, node.DataBytes);
            Assert.Equal(beta.Links.Count(), node.Links.Count());
            Assert.Equal(beta.Links.First().Id, node.Links.First().Id);
            Assert.Equal(beta.Links.First().Name, node.Links.First().Name);
            Assert.Equal(beta.Links.First().Size, node.Links.First().Size);
        }

        [Fact]
        public async Task Put_Get_Data()
        {
            var adata = Encoding.UTF8.GetBytes("alpha");
            var bdata = Encoding.UTF8.GetBytes("beta");
            var alpha = new DagNode(adata);
            var beta = await ipfs.Object.PutAsync(bdata, new[] {alpha.ToLink()});
            var node = await ipfs.Object.GetAsync(beta.Id);
            Assert.Equal(beta.DataBytes, node.DataBytes);
            Assert.Equal(beta.Links.Count(), node.Links.Count());
            Assert.Equal(beta.Links.First().Id, node.Links.First().Id);
            Assert.Equal(beta.Links.First().Name, node.Links.First().Name);
            Assert.Equal(beta.Links.First().Size, node.Links.First().Size);
        }

        [Fact]
        public async Task Data()
        {
            var adata = Encoding.UTF8.GetBytes("alpha");
            var node = await ipfs.Object.PutAsync(adata);
            using (var stream = await ipfs.Object.DataAsync(node.Id))
            {
                var bdata = new byte[adata.Length];
                stream.Read(bdata, 0, bdata.Length);
                Assert.Equal(adata, bdata);
            }
        }

        [Fact]
        public async Task Links()
        {
            var adata = Encoding.UTF8.GetBytes("alpha");
            var bdata = Encoding.UTF8.GetBytes("beta");
            var alpha = new DagNode(adata);
            var beta = await ipfs.Object.PutAsync(bdata, new[] {alpha.ToLink()});
            var links = await ipfs.Object.LinksAsync(beta.Id);
            Assert.Equal(beta.Links.Count(), links.Count());
            Assert.Equal(beta.Links.First().Id, links.First().Id);
            Assert.Equal(beta.Links.First().Name, links.First().Name);
            Assert.Equal(beta.Links.First().Size, links.First().Size);
        }

        [Fact]
        public async Task Stat()
        {
            var data1 = Encoding.UTF8.GetBytes("Some data 1");
            var data2 = Encoding.UTF8.GetBytes("Some data 2");
            var node2 = new DagNode(data2);
            var node1 = await ipfs.Object.PutAsync(data1,
                new[] {node2.ToLink("some-link")});
            var info = await ipfs.Object.StatAsync(node1.Id);
            Assert.Equal(1, info.LinkCount);
            Assert.Equal(11, info.DataSize);
            Assert.Equal(64, info.BlockSize);
            Assert.Equal(53, info.LinkSize);
            Assert.Equal(77, info.CumulativeSize);
        }

        [Fact]
        public async Task Get_Nonexistent()
        {
            var data = Encoding.UTF8.GetBytes("Some data for net-ipfs-engine-test that cannot be found");
            var node = new DagNode(data);
            var id = node.Id;
            var cs = new CancellationTokenSource(500);
            try
            {
                var _ = await ipfs.Object.GetAsync(id, cs.Token);
                throw new XunitException("Did not throw TaskCanceledException");
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

        [Fact]

        /// <seealso href="https://github.com/ipfs/js-ipfs/issues/2084"/>
        public async Task Get_Inlinefile()
        {
            var original = ipfs.Options.Block.AllowInlineCid;
            try
            {
                ipfs.Options.Block.AllowInlineCid = true;

                var node = await ipfs.FileSystem.AddTextAsync("hiya");
                Assert.Equal(1, node.Id.Version);
                Assert.True(node.Id.Hash.IsIdentityHash);

                var dag = await ipfs.Object.GetAsync(node.Id);
                Assert.Equal(12, dag.Size);
            }
            finally
            {
                ipfs.Options.Block.AllowInlineCid = original;
            }
        }

        [Fact]
        public async Task Links_InlineCid()
        {
            var original = ipfs.Options.Block.AllowInlineCid;
            try
            {
                ipfs.Options.Block.AllowInlineCid = true;

                var node = await ipfs.FileSystem.AddTextAsync("hiya");
                Assert.Equal(1, node.Id.Version);
                Assert.True(node.Id.Hash.IsIdentityHash);

                var links = await ipfs.Object.LinksAsync(node.Id);
                Assert.Equal(0, links.Count());
            }
            finally
            {
                ipfs.Options.Block.AllowInlineCid = original;
            }
        }

        [Fact]
        public async Task Links_RawCid()
        {
            var blob = new byte[2048];
            var cid = await ipfs.Block.PutAsync(blob, contentType: "raw");

            var links = await ipfs.Object.LinksAsync(cid);
            Assert.Equal(0, links.Count());
        }
    }
}
