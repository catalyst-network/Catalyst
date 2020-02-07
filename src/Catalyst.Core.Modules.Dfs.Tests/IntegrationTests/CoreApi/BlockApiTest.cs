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
using Lib.P2P;
using MultiFormats;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public sealed class BlockApiTest
    {
        private IDfsService _dfs;
        private readonly ITestOutputHelper _output;
        private const string Id = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";
        private readonly byte[] _blob = Encoding.UTF8.GetBytes("blorb");

        public BlockApiTest(ITestOutputHelper output)
        {
            _output = output;
            _dfs = TestDfs.GetTestDfs(output, null, "sha2-256");
        }
        
        [Fact]
        public async Task Put_Bytes()
        {
            var cid = await _dfs.BlockApi.PutAsync(_blob);
            Assert.Equal(Id, cid);

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(_blob.Length, data.Size);
            Assert.Equal(_blob, data.DataBytes);
        }

        [Fact]
        public void Put_Bytes_TooBig()
        {
            var data = new byte[_dfs.Options.Block.MaxBlockSize + 1];
            ExceptionAssert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var _ = _dfs.BlockApi.PutAsync(data).Result;
            });
        }

        [Fact]
        public void Put_Bytes_ContentType()
        {
            var cid = _dfs.BlockApi.PutAsync(_blob, "raw").Result;
            Assert.Equal("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", cid);

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(_blob.Length, data.Size);
            Assert.Equal(_blob, data.DataBytes);
        }

        [Fact]
        public void Put_Bytes_Inline_Cid()
        {
            try
            {
                _dfs.Options.Block.AllowInlineCid = true;
                var cid = _dfs.BlockApi.PutAsync(_blob, "raw").Result;
                Assert.True(cid.Hash.IsIdentityHash);
                Assert.Equal("bafkqablcnrxxeyq", cid);

                var data = _dfs.BlockApi.GetAsync(cid).Result;
                Assert.Equal(_blob.Length, data.Size);
                Assert.Equal(_blob, data.DataBytes);

                var content = new byte[_dfs.Options.Block.InlineCidLimit];
                cid = _dfs.BlockApi.PutAsync(content, "raw").Result;
                Assert.True(cid.Hash.IsIdentityHash);

                content = new byte[_dfs.Options.Block.InlineCidLimit + 1];
                cid = _dfs.BlockApi.PutAsync(content, "raw").Result;
                Assert.False(cid.Hash.IsIdentityHash);
            }
            finally
            {
                _dfs.Options.Block.AllowInlineCid = false;
            }
        }

        [Fact]
        public void Put_Bytes_Hash()
        {
            var cid = _dfs.BlockApi.PutAsync(_blob, "raw", "sha2-512").Result;
            Assert.Equal(
                "bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e",
                cid);

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(_blob.Length, data.Size);
            Assert.Equal(_blob, data.DataBytes);
        }

        [Fact]
        public void Put_Bytes_Cid_Encoding()
        {
            var cid = _dfs.BlockApi.PutAsync(_blob,
                "raw",
                encoding: "base32").Result;
            Assert.Equal(1, cid.Version);
            Assert.Equal("base32", cid.Encoding);

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(_blob.Length, data.Size);
            Assert.Equal(_blob, data.DataBytes);
        }

        [Fact]
        public void Put_Stream()
        {
            var cid = _dfs.BlockApi.PutAsync(new MemoryStream(_blob)).Result;
            Assert.Equal(Id, cid);

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(_blob.Length, data.Size);
            Assert.Equal(_blob, data.DataBytes);
        }

        [Fact]
        public void Put_Stream_ContentType()
        {
            var cid = _dfs.BlockApi.PutAsync(new MemoryStream(_blob), "raw").Result;
            Assert.Equal("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", cid);

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(_blob.Length, data.Size);
            Assert.Equal(_blob, data.DataBytes);
        }

        [Fact]
        public void Put_Stream_Hash()
        {
            var cid = _dfs.BlockApi.PutAsync(new MemoryStream(_blob), "raw", "sha2-512").Result;
            Assert.Equal(
                "bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e",
                cid);

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equal(_blob.Length, data.Size);
            Assert.Equal(_blob, data.DataBytes);
        }

        [Fact]
        public void Get()
        {
            var _ = _dfs.BlockApi.PutAsync(_blob).Result;
            var block = _dfs.BlockApi.GetAsync(Id).Result;
            Assert.Equal(Id, block.Id);
            Assert.Equal(_blob, block.DataBytes);
            var blob1 = new byte[_blob.Length];
            block.DataStream.Read(blob1, 0, blob1.Length);
            Assert.Equal(_blob, blob1);
        }

        [Fact]
        public void Stat()
        {
            var _ = _dfs.BlockApi.PutAsync(_blob).Result;
            var info = _dfs.BlockApi.StatAsync(Id).Result;
            Assert.Equal(Id, info.Id);
            Assert.Equal(5, info.Size);
        }

        [Fact]
        public async Task Stat_Inline_CID()
        {
            var cts = new CancellationTokenSource(300);
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(_blob, "identity")
            };
            var info = await _dfs.BlockApi.StatAsync(cid, cts.Token);
            Assert.Equal(cid.Encode(), info.Id);
            Assert.Equal(5, info.Size);
        }

        [Fact]
        public async Task Stat_Unknown()
        {
            const string cid = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF";
            var block = await _dfs.BlockApi.StatAsync(cid);
            Assert.Null(block);
        }

        [Fact]
        public async Task Remove()
        {
            var _ = _dfs.BlockApi.PutAsync(_blob).Result;
            var cid = await _dfs.BlockApi.RemoveAsync(Id);
            Assert.Equal(Id, cid);
        }

        [Fact]
        public async Task Remove_Inline_CID()
        {
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(_blob, "identity")
            };
            var removedCid = await _dfs.BlockApi.RemoveAsync(cid);
            Assert.Equal(cid.Encode(), removedCid.Encode());
        }

        [Fact]
        public void Remove_Unknown()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = _dfs.BlockApi.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF").Result;
            });
        }

        [Fact]
        public async Task Remove_Unknown_OK()
        {
            var cid = await _dfs.BlockApi.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF", true);
            Assert.Equal(null, cid);
        }

        [Fact]
        public async Task Get_Inline_CID()
        {
            var cts = new CancellationTokenSource(300);
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(_blob, "identity")
            };
            var block = await _dfs.BlockApi.GetAsync(cid, cts.Token);
            Assert.Equal(cid.Encode(), block.Id.Encode());
            Assert.Equal(_blob.Length, block.Size);
            Assert.Equal(_blob, block.DataBytes);
        }

        [Fact]
        public async Task Put_Informs_Bitswap()
        {
            _dfs = TestDfs.GetTestDfs(_output, null, "sha2-256");
            await _dfs.StartAsync();

            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = new Cid
                {
                    Hash = MultiHash.ComputeHash(data)
                };

                var cts = new CancellationTokenSource();
                cts.CancelAfter(20000);

                var wantTask = _dfs.BitSwapApi.GetAsync(cid, cts.Token);
                var cid1 = await _dfs.BlockApi.PutAsync(data, cancel: cts.Token);

                Assert.Equal(cid, cid1);
                Assert.Equal(cid, wantTask.Result.Id);
                Assert.Equal(data.Length, wantTask.Result.Size);
                Assert.Equal(data, wantTask.Result.DataBytes);
            }
            finally
            {
                await _dfs.StopAsync();
            }
        }

        [Fact]
        public async Task Put_Informs_Dht()
        {
            var data = Guid.NewGuid().ToByteArray();
            await _dfs.StartAsync();
            try
            {
                var self = _dfs.LocalPeer;
                var cid = await _dfs.BlockApi.PutAsync(data);
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var peers = await _dfs.DhtApi.FindProvidersAsync(cid, 1, cancel: cts.Token);
                Assert.Equal(self, peers.First());
            }
            finally
            {
                await _dfs.StopAsync();
            }
        }
    }
}
