#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Catalyst.Core.Modules.Hashing;
using MultiFormats.Registry;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class ObjectApiTest
    {
        private readonly IHashProvider _hashProvider;
        private IDfsService ipfs;

        public ObjectApiTest(ITestOutputHelper output)
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("sha2-256"));
            ipfs = TestDfs.GetTestDfs(output, null, null, _hashProvider);
        }

        [Fact]
        public async Task New_Template_Null()
        {
            var node = await ipfs.ObjectApi.NewAsync();
            Assert.Equal("QmdfTbBqBPQ7VNxZEYEj14VmRuZBkqFbiwReogJgS1zR1n", (string) node.Id);
        }

        [Fact]
        public async Task New_Template_UnixfsDir()
        {
            var node = await ipfs.ObjectApi.NewAsync("unixfs-dir");
            Assert.Equal("QmUNLLsPACCz1vLxQVkXqqLX5R1X345qqfHbsf67hvA3Nn", (string) node.Id);

            node = await ipfs.ObjectApi.NewDirectoryAsync();
            Assert.Equal("QmUNLLsPACCz1vLxQVkXqqLX5R1X345qqfHbsf67hvA3Nn", (string) node.Id);
        }

        [Fact]
        public void New_Template_Unknown()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var node = ipfs.ObjectApi.NewAsync("unknown-template").Result;
            });
        }

        [Fact]
        public async Task Put_Get_Dag()
        {
            var adata = Encoding.UTF8.GetBytes("alpha");
            var bdata = Encoding.UTF8.GetBytes("beta");
            var alpha = new DagNode(adata);
            var beta = new DagNode(bdata, new[] {alpha.ToLink()});
            var x = await ipfs.ObjectApi.PutAsync(beta);
            var node = await ipfs.ObjectApi.GetAsync(x.Id);
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
            var beta = await ipfs.ObjectApi.PutAsync(bdata, new[] {alpha.ToLink()});
            var node = await ipfs.ObjectApi.GetAsync(beta.Id);
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
            var node = await ipfs.ObjectApi.PutAsync(adata);
            using (var stream = await ipfs.ObjectApi.DataAsync(node.Id))
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
            var beta = await ipfs.ObjectApi.PutAsync(bdata, new[] {alpha.ToLink()});
            var links = await ipfs.ObjectApi.LinksAsync(beta.Id);
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
            var node1 = await ipfs.ObjectApi.PutAsync(data1,
                new[] {node2.ToLink("some-link")});
            var info = await ipfs.ObjectApi.StatAsync(node1.Id);
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
                var _ = await ipfs.ObjectApi.GetAsync(id, cs.Token);
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

                var node = await ipfs.UnixFsApi.AddTextAsync("hiya");
                Assert.Equal(1, node.Id.Version);
                Assert.True(node.Id.Hash.IsIdentityHash);

                var dag = await ipfs.ObjectApi.GetAsync(node.Id);
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

                var node = await ipfs.UnixFsApi.AddTextAsync("hiya");
                Assert.Equal(1, node.Id.Version);
                Assert.True(node.Id.Hash.IsIdentityHash);

                var links = await ipfs.ObjectApi.LinksAsync(node.Id);
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
            var cid = await ipfs.BlockApi.PutAsync(blob, contentType: "raw");

            var links = await ipfs.ObjectApi.LinksAsync(cid);
            Assert.Equal(0, links.Count());
        }
    }
}
