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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Catalyst.Core.Modules.Hashing;
using Lib.P2P;
using MultiFormats;
using MultiFormats.Registry;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public sealed class BlockApiTest
    {
        private IDfsService ipfs;
        private readonly ITestOutputHelper _output;
        private string id = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";
        private byte[] blob = Encoding.UTF8.GetBytes("blorb");

        public BlockApiTest(ITestOutputHelper output)
        {
            _output = output;
            ipfs = TestDfs.GetTestDfs(output, null, null, new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("sha2-256")));
        }
        
        [Fact]
        public async Task Put_Bytes()
        {
            var cid = await ipfs.BlockApi.PutAsync(blob);
            Assert.Equal(id, cid);

            var data = ipfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Bytes_TooBig()
        {
            var data = new byte[ipfs.Options.Block.MaxBlockSize + 1];
            ExceptionAssert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var _ = ipfs.BlockApi.PutAsync(data).Result;
            });
        }

        [Fact]
        public void Put_Bytes_ContentType()
        {
            var cid = ipfs.BlockApi.PutAsync(blob, "raw").Result;
            Assert.Equal("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", cid);

            var data = ipfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Bytes_Inline_Cid()
        {
            try
            {
                ipfs.Options.Block.AllowInlineCid = true;
                var cid = ipfs.BlockApi.PutAsync(blob, "raw").Result;
                Assert.True(cid.Hash.IsIdentityHash);
                Assert.Equal("bafkqablcnrxxeyq", cid);

                var data = ipfs.BlockApi.GetAsync(cid).Result;
                Assert.Equal(blob.Length, data.Size);
                Assert.Equal(blob, data.DataBytes);

                var content = new byte[ipfs.Options.Block.InlineCidLimit];
                cid = ipfs.BlockApi.PutAsync(content, "raw").Result;
                Assert.True(cid.Hash.IsIdentityHash);

                content = new byte[ipfs.Options.Block.InlineCidLimit + 1];
                cid = ipfs.BlockApi.PutAsync(content, "raw").Result;
                Assert.False(cid.Hash.IsIdentityHash);
            }
            finally
            {
                ipfs.Options.Block.AllowInlineCid = false;
            }
        }

        [Fact]
        public void Put_Bytes_Hash()
        {
            var cid = ipfs.BlockApi.PutAsync(blob, "raw", "sha2-512").Result;
            Assert.Equal(
                "bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e",
                cid);

            var data = ipfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Bytes_Cid_Encoding()
        {
            var cid = ipfs.BlockApi.PutAsync(blob,
                "raw",
                encoding: "base32").Result;
            Assert.Equal(1, cid.Version);
            Assert.Equal("base32", cid.Encoding);

            var data = ipfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Stream()
        {
            var cid = ipfs.BlockApi.PutAsync(new MemoryStream(blob)).Result;
            Assert.Equal(id, cid);

            var data = ipfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Stream_ContentType()
        {
            var cid = ipfs.BlockApi.PutAsync(new MemoryStream(blob), "raw").Result;
            Assert.Equal("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", cid);

            var data = ipfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Stream_Hash()
        {
            var cid = ipfs.BlockApi.PutAsync(new MemoryStream(blob), "raw", "sha2-512").Result;
            Assert.Equal(
                "bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e",
                cid);

            var data = ipfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Get()
        {
            var _ = ipfs.BlockApi.PutAsync(blob).Result;
            var block = ipfs.BlockApi.GetAsync(id).Result;
            Assert.Equal(id, block.Id);
            Assert.Equal(blob, block.DataBytes);
            var blob1 = new byte[blob.Length];
            block.DataStream.Read(blob1, 0, blob1.Length);
            Assert.Equal(blob, blob1);
        }

        [Fact]
        public void Stat()
        {
            var _ = ipfs.BlockApi.PutAsync(blob).Result;
            var info = ipfs.BlockApi.StatAsync(id).Result;
            Assert.Equal(id, info.Id);
            Assert.Equal(5, info.Size);
        }

        [Fact]
        public async Task Stat_Inline_CID()
        {
            var cts = new CancellationTokenSource(300);
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(blob, "identity")
            };
            var info = await ipfs.BlockApi.StatAsync(cid, cts.Token);
            Assert.Equal(cid.Encode(), info.Id);
            Assert.Equal(5, info.Size);
        }

        [Fact]
        public async Task Stat_Unknown()
        {
            var cid = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF";
            var block = await ipfs.BlockApi.StatAsync(cid);
            Assert.Null(block);
        }

        [Fact]
        public async Task Remove()
        {
            var _ = ipfs.BlockApi.PutAsync(blob).Result;
            var cid = await ipfs.BlockApi.RemoveAsync(id);
            Assert.Equal(id, cid);
        }

        [Fact]
        public async Task Remove_Inline_CID()
        {
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(blob, "identity")
            };
            var removedCid = await ipfs.BlockApi.RemoveAsync(cid);
            Assert.Equal(cid.Encode(), removedCid.Encode());
        }

        [Fact]
        public void Remove_Unknown()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ipfs.BlockApi.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF").Result;
            });
        }

        [Fact]
        public async Task Remove_Unknown_OK()
        {
            var cid = await ipfs.BlockApi.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF", true);
            Assert.Equal(null, cid);
        }

        [Fact]
        public async Task Get_Inline_CID()
        {
            var cts = new CancellationTokenSource(300);
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(blob, "identity")
            };
            var block = await ipfs.BlockApi.GetAsync(cid, cts.Token);
            Assert.Equal(cid.Encode(), block.Id.Encode());
            Assert.Equal(blob.Length, block.Size);
            Assert.Equal(blob, block.DataBytes);
        }

        [Fact]
        public async Task Put_Informs_Bitswap()
        {
            ipfs = TestDfs.GetTestDfs(_output, null, null, new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("sha2-256")));
            await ipfs.StartAsync();

            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = new Cid
                {
                    Hash = MultiHash.ComputeHash(data)
                };

                var wantTask = ipfs.BitSwapApi.GetAsync(cid);

                var cid1 = await ipfs.BlockApi.PutAsync(data);
                Assert.Equal(cid, cid1);
                Assert.Equal(cid, wantTask.Result.Id);
                Assert.Equal(data.Length, wantTask.Result.Size);
                Assert.Equal(data, wantTask.Result.DataBytes);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [Fact]
        public async Task Put_Informs_Dht()
        {
            var data = Guid.NewGuid().ToByteArray();
            await ipfs.StartAsync();
            try
            {
                var self = ipfs.LocalPeer;
                var cid = await ipfs.BlockApi.PutAsync(data);
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var peers = await ipfs.DhtApi.FindProvidersAsync(cid, 1, cancel: cts.Token);
                Assert.Equal(self, peers.First());
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }
    }
}
