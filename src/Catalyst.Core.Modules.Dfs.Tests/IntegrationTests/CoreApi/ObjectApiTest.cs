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
using Catalyst.Core.Lib.Dag;
using Catalyst.TestUtils;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class ObjectApiTest
    {
        private IDfsService ipfs;

        public ObjectApiTest()
        {
            ipfs = TestDfs.GetTestDfs(null, "sha2-256");
        }

        [Test]
        public async Task New_Template_Null()
        {
            var node = await ipfs.ObjectApi.NewAsync();
            Assert.AreEqual("QmdfTbBqBPQ7VNxZEYEj14VmRuZBkqFbiwReogJgS1zR1n", node.Id.ToString());
        }

        [Test]
        public async Task New_Template_UnixfsDir()
        {
            var node = await ipfs.ObjectApi.NewAsync("unixfs-dir");
            Assert.AreEqual("QmUNLLsPACCz1vLxQVkXqqLX5R1X345qqfHbsf67hvA3Nn", node.Id.ToString());

            node = await ipfs.ObjectApi.NewDirectoryAsync();
            Assert.AreEqual("QmUNLLsPACCz1vLxQVkXqqLX5R1X345qqfHbsf67hvA3Nn", node.Id.ToString());
        }

        [Test]
        public void New_Template_Unknown()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                ipfs.ObjectApi.NewAsync("unknown-template").GetAwaiter().GetResult();
            });
        }

        [Test]
        public async Task Put_Get_Dag()
        {
            var adata = Encoding.UTF8.GetBytes("alpha");
            var bdata = Encoding.UTF8.GetBytes("beta");
            var alpha = new DagNode(adata);
            var beta = new DagNode(bdata, new[] {alpha.ToLink()});
            var x = await ipfs.ObjectApi.PutAsync(beta);
            var node = await ipfs.ObjectApi.GetAsync(x.Id);
            Assert.AreEqual(beta.DataBytes, node.DataBytes);
            Assert.AreEqual(beta.Links.Count(), node.Links.Count());
            Assert.AreEqual(beta.Links.First().Id, node.Links.First().Id);
            Assert.AreEqual(beta.Links.First().Name, node.Links.First().Name);
            Assert.AreEqual(beta.Links.First().Size, node.Links.First().Size);
        }

        [Test]
        public async Task Put_Get_Data()
        {
            var adata = Encoding.UTF8.GetBytes("alpha");
            var bdata = Encoding.UTF8.GetBytes("beta");
            var alpha = new DagNode(adata);
            var beta = await ipfs.ObjectApi.PutAsync(bdata, new[] {alpha.ToLink()});
            var node = await ipfs.ObjectApi.GetAsync(beta.Id);
            Assert.AreEqual(beta.DataBytes, node.DataBytes);
            Assert.AreEqual(beta.Links.Count(), node.Links.Count());
            Assert.AreEqual(beta.Links.First().Id, node.Links.First().Id);
            Assert.AreEqual(beta.Links.First().Name, node.Links.First().Name);
            Assert.AreEqual(beta.Links.First().Size, node.Links.First().Size);
        }

        [Test]
        public async Task Data()
        {
            var adata = Encoding.UTF8.GetBytes("alpha");
            var node = await ipfs.ObjectApi.PutAsync(adata);
            await using (var stream = await ipfs.ObjectApi.DataAsync(node.Id))
            {
                var bdata = new byte[adata.Length];
                stream.Read(bdata, 0, bdata.Length);
                Assert.AreEqual(adata, bdata);
            }
        }

        [Test]
        public async Task Links()
        {
            var adata = Encoding.UTF8.GetBytes("alpha");
            var bdata = Encoding.UTF8.GetBytes("beta");
            var alpha = new DagNode(adata);
            var beta = await ipfs.ObjectApi.PutAsync(bdata, new[] {alpha.ToLink()});
            var links = await ipfs.ObjectApi.LinksAsync(beta.Id);
            Assert.AreEqual(beta.Links.Count(), links.Count());
            Assert.AreEqual(beta.Links.First().Id, links.First().Id);
            Assert.AreEqual(beta.Links.First().Name, links.First().Name);
            Assert.AreEqual(beta.Links.First().Size, links.First().Size);
        }

        [Test]
        public async Task Stat()
        {
            var data1 = Encoding.UTF8.GetBytes("Some data 1");
            var data2 = Encoding.UTF8.GetBytes("Some data 2");
            var node2 = new DagNode(data2);
            var node1 = await ipfs.ObjectApi.PutAsync(data1,
                new[] {node2.ToLink("some-link")});
            var info = await ipfs.ObjectApi.StatAsync(node1.Id);
            Assert.AreEqual(1, info.LinkCount);
            Assert.AreEqual(11, info.DataSize);
            Assert.AreEqual(64, info.BlockSize);
            Assert.AreEqual(53, info.LinkSize);
            Assert.AreEqual(77, info.CumulativeSize);
        }

        [Test]
        public async Task Get_Nonexistent()
        {
            var data = Encoding.UTF8.GetBytes("Some data for net-ipfs-engine-test that cannot be found");
            var node = new DagNode(data);
            var id = node.Id;
            var cs = new CancellationTokenSource(500);
            try
            {
                var _ = await ipfs.ObjectApi.GetAsync(id, cs.Token);
                throw new Exception("Did not throw TaskCanceledException");
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
        }

        [Test]

        /// <seealso href="https://github.com/ipfs/js-ipfs/issues/2084"/>
        public async Task Get_Inlinefile()
        {
            var original = ipfs.Options.Block.AllowInlineCid;
            try
            {
                ipfs.Options.Block.AllowInlineCid = true;

                var node = await ipfs.UnixFsApi.AddTextAsync("hiya");
                Assert.AreEqual(1, node.Id.Version);
                Assert.True(node.Id.Hash.IsIdentityHash);

                var dag = await ipfs.ObjectApi.GetAsync(node.Id);
                Assert.AreEqual(12, dag.Size);
            }
            finally
            {
                ipfs.Options.Block.AllowInlineCid = original;
            }
        }

        [Test]
        public async Task Links_InlineCid()
        {
            var original = ipfs.Options.Block.AllowInlineCid;
            try
            {
                ipfs.Options.Block.AllowInlineCid = true;

                var node = await ipfs.UnixFsApi.AddTextAsync("hiya");
                Assert.AreEqual(1, node.Id.Version);
                Assert.True(node.Id.Hash.IsIdentityHash);

                var links = await ipfs.ObjectApi.LinksAsync(node.Id);
                Assert.AreEqual(0, links.Count());
            }
            finally
            {
                ipfs.Options.Block.AllowInlineCid = original;
            }
        }

        [Test]
        public async Task Links_RawCid()
        {
            var blob = new byte[2048];
            var cid = await ipfs.BlockApi.PutAsync(blob, "raw");

            var links = await ipfs.ObjectApi.LinksAsync(cid);
            Assert.AreEqual(0, links.Count());
        }
    }
}
