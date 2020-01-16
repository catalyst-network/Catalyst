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
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Catalyst.Core.Modules.Hashing;
using Lib.P2P;
using MultiFormats;
using MultiFormats.Registry;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class PinApiTest
    {
        private IDfsService ipfs;
        private readonly IHashProvider _hashProvider;

        public PinApiTest(ITestOutputHelper output)
        {
            ipfs = TestDfs.GetTestDfs(output);
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
        }

        [Fact]
        public async Task Add_Remove()
        {
            var result = await ipfs.UnixFsApi.AddTextAsync("I am pinned");
            var id = result.Id;

            var pins = await ipfs.PinApi.AddAsync(id);
            Assert.True(pins.Any(pin => pin == id));
            var all = await ipfs.PinApi.ListAsync();
            Assert.True(all.Any(pin => pin == id));

            pins = await ipfs.PinApi.RemoveAsync(id);
            Assert.True(pins.Any(pin => pin == id));
            all = await ipfs.PinApi.ListAsync();
            Assert.False(all.Any(pin => pin == id));
        }

        [Fact]
        public async Task Remove_Unknown()
        {
            var dag = new DagNode(Encoding.UTF8.GetBytes("some unknown info for net-ipfs-engine-pin-test"));
            await ipfs.PinApi.RemoveAsync(dag.Id, true);
        }

        [Fact]
        public async Task Inline_Cid()
        {
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(new byte[]
                {
                    1, 2, 3
                }, "identity")
            };
            var pins = await ipfs.PinApi.AddAsync(cid, recursive: false);
            Assert.Contains(cid, pins.ToArray());
            var all = await ipfs.PinApi.ListAsync();
            Assert.Contains(cid, all.ToArray());

            var removals = await ipfs.PinApi.RemoveAsync(cid, recursive: false);
            Assert.Contains(cid, removals.ToArray());
            all = await ipfs.PinApi.ListAsync();
            Assert.DoesNotContain(cid, all.ToArray());
        }

        [Fact]
        public void Add_Unknown()
        {
            var dag = new DagNode(Encoding.UTF8.GetBytes("some unknown info for net-ipfs-engine-pin-test"));
            ExceptionAssert.Throws<Exception>(() =>
            {
                var cts = new CancellationTokenSource(250);
                var _ = ipfs.PinApi.AddAsync(dag.Id, true, cts.Token).Result;
            });
        }

        [Fact]
        public async Task Add_Recursive()
        {
            var options = new AddFileOptions
            {
                ChunkSize = 3,
                Pin = false,
                RawLeaves = true,
                Wrap = true,
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var cids = await ipfs.PinApi.AddAsync(node.Id, true);
            Assert.Equal(6, cids.Count());
        }

        [Fact]
        public async Task Remove_Recursive()
        {
            var options = new AddFileOptions
            {
                ChunkSize = 3,
                Pin = false,
                RawLeaves = true,
                Wrap = true,
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var cids = await ipfs.PinApi.AddAsync(node.Id, true);
            Assert.Equal(6, cids.Count());

            var removedCids = await ipfs.PinApi.RemoveAsync(node.Id, true);
            Assert.Equal(cids.ToArray(), removedCids.ToArray());
        }
    }
}