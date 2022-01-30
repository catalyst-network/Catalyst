#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Text;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.TestUtils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public sealed class DagApiTest
    {
        private readonly byte[] _blob = Encoding.UTF8.GetBytes("blorb");
        private const string Blob64 = "YmxvcmI"; // base 64 encoded with no padding
        private readonly IDfsService _dfs;

        public DagApiTest()
        {
            _dfs = TestDfs.GetTestDfs(null, "sha2-256");
        }

        [Test]
        public async Task Get_Raw()
        {
            var cid = await _dfs.BlockApi.PutAsync(_blob, contentType: "raw");
            Assert.AreEqual("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", cid.ToString());

            var dag = await _dfs.DagApi.GetAsync(cid);
            Assert.AreEqual(Blob64, (string) dag["data"]);
        }

        private sealed class Name
        {
            public string First { get; set; }
            public string Last { get; set; }
        }

        [Test]
        public async Task PutAndGet_JSON()
        {
            var expected = new JObject {["a"] = "alpha"};
            const string expectedId = "bafyreigdhej736dobd6z3jt2vxsxvbwrwgyts7e7wms6yrr46rp72uh5bu";
            var id = await _dfs.DagApi.PutAsync(expected);
            Assert.NotNull(id);
            Assert.AreEqual(expectedId, id.ToString());

            var actual = await _dfs.DagApi.GetAsync(id);
            Assert.NotNull(actual);
            Assert.AreEqual(expected["a"], actual["a"]);

            var value = (string) await _dfs.DagApi.GetAsync(expectedId + "/a");
            Assert.AreEqual(expected["a"].ToString(), value);
        }

        [Test]
        public async Task PutAndGet_poco()
        {
            var expected = new Name {First = "John", Last = "Smith"};
            var id = await _dfs.DagApi.PutAsync(expected);
            Assert.NotNull(id);

            var actual = await _dfs.DagApi.GetAsync<Name>(id);
            Assert.NotNull(actual);
            Assert.AreEqual(expected.First, actual.First);
            Assert.AreEqual(expected.Last, actual.Last);

            var value = (string) await _dfs.DagApi.GetAsync(id.Encode() + "/Last");
            Assert.AreEqual(expected.Last, value);
        }

        /// <summary>
        /// @TODO hardcoded encoding types
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task PutAndGet_poco_CidEncoding()
        {
            var expected = new Name {First = "John", Last = "Smith"};
            var id = await _dfs.DagApi.PutAsync(expected, encoding: "base32");
            Assert.NotNull(id);
            Assert.AreEqual("base32", id.Encoding);
            Assert.AreEqual(1, id.Version);

            var actual = await _dfs.DagApi.GetAsync<Name>(id);
            Assert.NotNull(actual);
            Assert.AreEqual(expected.First, actual.First);
            Assert.AreEqual(expected.Last, actual.Last);

            var value = (string) await _dfs.DagApi.GetAsync(id.Encode() + "/Last");
            Assert.AreEqual(expected.Last, value);
        }

        [Test]
        public async Task PutAndGet_POCO()
        {
            var expected = new Name {First = "John", Last = "Smith"};
            var id = await _dfs.DagApi.PutAsync(expected);
            Assert.NotNull(id);

            var actual = await _dfs.DagApi.GetAsync<Name>(id);
            Assert.NotNull(actual);
            Assert.AreEqual(expected.First, actual.First);
            Assert.AreEqual(expected.Last, actual.Last);

            var value = (string) await _dfs.DagApi.GetAsync(id.Encode() + "/Last");
            Assert.AreEqual(expected.Last, value);
        }

        [Test]
        public async Task Get_Raw2()
        {
            var data = Encoding.UTF8.GetBytes("abc");
            var id = await _dfs.BlockApi.PutAsync(data, "raw");
            Assert.AreEqual("bafkreif2pall7dybz7vecqka3zo24irdwabwdi4wc55jznaq75q7eaavvu", id.Encode());

            var actual = await _dfs.DagApi.GetAsync(id);
            Assert.AreEqual(Convert.ToBase64String(data), (string) actual["data"]);
        }

        // // https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/DAG.md
        // [Test]
        // [Ignore("https://github.com/richardschneider/net-ipfs-engine/issues/30")]
        // public async Task Example1()
        // {
        //     Cid expected =
        //         "zBwWX9ecx5F4X54WAjmFLErnBT6ByfNxStr5ovowTL7AhaUR98RWvXPS1V3HqV1qs3r5Ec5ocv7eCdbqYQREXNUfYNuKG";
        //     var obj = new {simple = "object"};
        //     var cid = await ipfs.Dag.PutAsync(obj, multiHash: "sha3-512");
        //     Assert.AreEqual((string) expected, (string) cid);
        // }
    }
}
