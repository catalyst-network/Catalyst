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

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ipfs.Dispose();
        }

        public ObjectApiTest()
        {
            ipfs = TestDfs.GetTestDfs(null, "sha2-256");
        }

        [Test]
        public async Task New_Template_Null()
        {
            var node = await ipfs.ObjectApi.NewAsync();
            Assert.That(node.Id.ToString(), Is.EqualTo("QmdfTbBqBPQ7VNxZEYEj14VmRuZBkqFbiwReogJgS1zR1n"));
        }

        [Test]
        public async Task New_Template_UnixfsDir()
        {
            var node = await ipfs.ObjectApi.NewAsync("unixfs-dir");
            Assert.That(node.Id.ToString(), Is.EqualTo("QmUNLLsPACCz1vLxQVkXqqLX5R1X345qqfHbsf67hvA3Nn"));

            node = await ipfs.ObjectApi.NewDirectoryAsync();
            Assert.That(node.Id.ToString(), Is.EqualTo("QmUNLLsPACCz1vLxQVkXqqLX5R1X345qqfHbsf67hvA3Nn"));
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
            Assert.That(beta.DataBytes, Is.EqualTo(node.DataBytes));
            Assert.That(beta.Links.Count(), Is.EqualTo(node.Links.Count()));
            Assert.That(beta.Links.First().Id, Is.EqualTo(node.Links.First().Id));
            Assert.That(beta.Links.First().Name, Is.EqualTo(node.Links.First().Name));
            Assert.That(beta.Links.First().Size, Is.EqualTo(node.Links.First().Size));
        }

        [Test]
        public async Task Put_Get_Data()
        {
            var adata = Encoding.UTF8.GetBytes("alpha");
            var bdata = Encoding.UTF8.GetBytes("beta");
            var alpha = new DagNode(adata);
            var beta = await ipfs.ObjectApi.PutAsync(bdata, new[] {alpha.ToLink()});
            var node = await ipfs.ObjectApi.GetAsync(beta.Id);
            Assert.That(beta.DataBytes, Is.EqualTo(node.DataBytes));
            Assert.That(beta.Links.Count(), Is.EqualTo(node.Links.Count()));
            Assert.That(beta.Links.First().Id, Is.EqualTo(node.Links.First().Id));
            Assert.That(beta.Links.First().Name, Is.EqualTo(node.Links.First().Name));
            Assert.That(beta.Links.First().Size, Is.EqualTo(node.Links.First().Size));
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
                Assert.That(adata, Is.EqualTo(bdata));
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
            Assert.That(beta.Links.Count(), Is.EqualTo(links.Count()));
            Assert.That(beta.Links.First().Id, Is.EqualTo(links.First().Id));
            Assert.That(beta.Links.First().Name, Is.EqualTo(links.First().Name));
            Assert.That(beta.Links.First().Size, Is.EqualTo(links.First().Size));
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
            Assert.That(info.LinkCount, Is.EqualTo(1));
            Assert.That(info.DataSize, Is.EqualTo(11));
            Assert.That(info.BlockSize, Is.EqualTo(64));
            Assert.That(info.LinkSize, Is.EqualTo(53));
            Assert.That(info.CumulativeSize, Is.EqualTo(77));
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
                Assert.That(node.Id.Version, Is.EqualTo(1));
                Assert.That(node.Id.Hash.IsIdentityHash, Is.True);

                var dag = await ipfs.ObjectApi.GetAsync(node.Id);
                Assert.That(dag.Size, Is.EqualTo(12));
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
                Assert.That(node.Id.Version, Is.EqualTo(1));
                Assert.That(node.Id.Hash.IsIdentityHash, Is.True);

                var links = await ipfs.ObjectApi.LinksAsync(node.Id);
                Assert.That(links.Count(), Is.EqualTo(0));
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
            Assert.That(links.Count(), Is.EqualTo(0));
        }
    }
}
