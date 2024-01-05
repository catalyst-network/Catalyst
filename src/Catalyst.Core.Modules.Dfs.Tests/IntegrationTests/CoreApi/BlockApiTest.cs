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
using Catalyst.TestUtils;
using Lib.P2P;
using MultiFormats;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public sealed class BlockApiTest
    {
        private IDfsService _dfs;
        private const string Id = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";
        private readonly byte[] _blob = Encoding.UTF8.GetBytes("blorb");

        [TearDown]
        public void TearDown()
        {
            _dfs.Dispose();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _dfs.Dispose();
        }

        public BlockApiTest()
        {
            _dfs = TestDfs.GetTestDfs(null, "sha2-256");
        }
        
        [Test]
        public async Task Put_Bytes()
        {
            var cid = await _dfs.BlockApi.PutAsync(_blob);
            Assert.Equals(Id, cid.ToString());

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equals(_blob.Length, data.Size);
            Assert.Equals(_blob, data.DataBytes);
        }

        [Test]
        public void Put_Bytes_TooBig()
        {
            var data = new byte[_dfs.Options.Block.MaxBlockSize + 1];
            ExceptionAssert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var _ = _dfs.BlockApi.PutAsync(data).Result;
            });
        }

        [Test]
        public void Put_Bytes_ContentType()
        {
            var cid = _dfs.BlockApi.PutAsync(_blob, "raw").Result;
            Assert.Equals("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", cid.ToString());

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equals(_blob.Length, data.Size);
            Assert.Equals(_blob, data.DataBytes);
        }

        [Test]
        public void Put_Bytes_Inline_Cid()
        {
            try
            {
                _dfs.Options.Block.AllowInlineCid = true;
                var cid = _dfs.BlockApi.PutAsync(_blob, "raw").Result;
                Assert.That(cid.Hash.IsIdentityHash, Is.True);
                Assert.Equals("bafkqablcnrxxeyq", cid.ToString());

                var data = _dfs.BlockApi.GetAsync(cid).Result;
                Assert.Equals(_blob.Length, data.Size);
                Assert.Equals(_blob, data.DataBytes);

                var content = new byte[_dfs.Options.Block.InlineCidLimit];
                cid = _dfs.BlockApi.PutAsync(content, "raw").Result;
                Assert.That(cid.Hash.IsIdentityHash, Is.True);

                content = new byte[_dfs.Options.Block.InlineCidLimit + 1];
                cid = _dfs.BlockApi.PutAsync(content, "raw").Result;
                Assert.That(cid.Hash.IsIdentityHash, Is.False);
            }
            finally
            {
                _dfs.Options.Block.AllowInlineCid = false;
            }
        }

        [Test]
        public void Put_Bytes_Hash()
        {
            var cid = _dfs.BlockApi.PutAsync(_blob, "raw", "sha2-512").Result;
            Assert.Equals(
                "bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e",
                cid.ToString());

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equals(_blob.Length, data.Size);
            Assert.Equals(_blob, data.DataBytes);
        }

        [Test]
        public void Put_Bytes_Cid_Encoding()
        {
            var cid = _dfs.BlockApi.PutAsync(_blob,
                "raw",
                encoding: "base32").Result;
            Assert.Equals(1, cid.Version);
            Assert.Equals("base32", cid.Encoding);

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equals(_blob.Length, data.Size);
            Assert.Equals(_blob, data.DataBytes);
        }

        [Test]
        public void Put_Stream()
        {
            var cid = _dfs.BlockApi.PutAsync(new MemoryStream(_blob)).Result;
            Assert.Equals(Id, cid.ToString());

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equals(_blob.Length, data.Size);
            Assert.Equals(_blob, data.DataBytes);
        }

        [Test]
        public void Put_Stream_ContentType()
        {
            var cid = _dfs.BlockApi.PutAsync(new MemoryStream(_blob), "raw").Result;
            Assert.Equals("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", cid.ToString());

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equals(_blob.Length, data.Size);
            Assert.Equals(_blob, data.DataBytes);
        }

        [Test]
        public void Put_Stream_Hash()
        {
            var cid = _dfs.BlockApi.PutAsync(new MemoryStream(_blob), "raw", "sha2-512").Result;
            Assert.Equals(
                "bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e",
                cid.ToString());

            var data = _dfs.BlockApi.GetAsync(cid).Result;
            Assert.Equals(_blob.Length, data.Size);
            Assert.Equals(_blob, data.DataBytes);
        }

        [Test]
        public void Get()
        {
            var _ = _dfs.BlockApi.PutAsync(_blob).Result;
            var block = _dfs.BlockApi.GetAsync(Id).Result;
            Assert.Equals(Id, block.Id.ToString());
            Assert.Equals(_blob, block.DataBytes);
            var blob1 = new byte[_blob.Length];
            block.DataStream.Read(blob1, 0, blob1.Length);
            Assert.Equals(_blob, blob1);
        }

        [Test]
        public void Stat()
        {
            var _ = _dfs.BlockApi.PutAsync(_blob).Result;
            var info = _dfs.BlockApi.StatAsync(Id).Result;
            Assert.Equals(Id, info.Id.ToString());
            Assert.Equals(5, info.Size);
        }

        [Test]
        public async Task Stat_Inline_CID()
        {
            var cts = new CancellationTokenSource(300);
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(_blob, "identity")
            };
            var info = await _dfs.BlockApi.StatAsync(cid, cts.Token);
            Assert.Equals(cid.Encode(), info.Id.ToString());
            Assert.Equals(5, info.Size);
        }

        [Test]
        public async Task Stat_Unknown()
        {
            const string cid = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF";
            var block = await _dfs.BlockApi.StatAsync(cid);
            Assert.That(block, Is.Null);
        }

        [Test]
        public async Task Remove()
        {
            var _ = _dfs.BlockApi.PutAsync(_blob).Result;
            var cid = await _dfs.BlockApi.RemoveAsync(Id);
            Assert.Equals(Id, cid.ToString());
        }

        [Test]
        public async Task Remove_Inline_CID()
        {
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(_blob, "identity")
            };
            var removedCid = await _dfs.BlockApi.RemoveAsync(cid);
            Assert.Equals(cid.Encode(), removedCid.Encode());
        }

        [Test]
        public void Remove_Unknown()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = _dfs.BlockApi.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF").Result;
            });
        }

        [Test]
        public async Task Remove_Unknown_OK()
        {
            var cid = await _dfs.BlockApi.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF", true);
            Assert.Equals(null, cid);
        }

        [Test]
        public async Task Get_Inline_CID()
        {
            var cts = new CancellationTokenSource(300);
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(_blob, "identity")
            };
            var block = await _dfs.BlockApi.GetAsync(cid, cts.Token);
            Assert.Equals(cid.Encode(), block.Id.Encode());
            Assert.Equals(_blob.Length, block.Size);
            Assert.Equals(_blob, block.DataBytes);
        }

        [Test]
        public async Task Put_Informs_Bitswap()
        {
            _dfs = TestDfs.GetTestDfs(null, "sha2-256");
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

                Assert.Equals(cid, cid1);
                Assert.Equals(cid, wantTask.Result.Id);
                Assert.Equals(data.Length, wantTask.Result.Size);
                Assert.Equals(data, wantTask.Result.DataBytes);
            }
            finally
            {
                await _dfs.StopAsync();
            }
        }

        [Test]
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
                Assert.Equals(self, peers.First());
            }
            finally
            {
                await _dfs.StopAsync();
            }
        }
    }
}
