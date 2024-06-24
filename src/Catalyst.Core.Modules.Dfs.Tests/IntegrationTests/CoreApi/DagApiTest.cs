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

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _dfs.Dispose();
        }

        public DagApiTest()
        {
            _dfs = TestDfs.GetTestDfs(null, "sha2-256");
        }

        [Test]
        public async Task Get_Raw()
        {
            var cid = await _dfs.BlockApi.PutAsync(_blob, contentType: "raw");
            Assert.That(cid.ToString(), Is.EqualTo("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu"));

            var dag = await _dfs.DagApi.GetAsync(cid);
            Assert.That((string) dag["data"], Is.EqualTo(Blob64));
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
            Assert.That(id, Is.Not.Null);
            Assert.That(id.ToString(), Is.EqualTo(expectedId));

            var actual = await _dfs.DagApi.GetAsync(id);
            Assert.That(actual, Is.Not.Null);
            Assert.That(expected["a"], Is.EqualTo(actual["a"]));

            var value = (string) await _dfs.DagApi.GetAsync(expectedId + "/a");
            Assert.That(expected["a"].ToString(), Is.EqualTo(value));
        }

        [Test]
        public async Task PutAndGet_poco()
        {
            var expected = new Name {First = "John", Last = "Smith"};
            var id = await _dfs.DagApi.PutAsync(expected);
            Assert.That(id, Is.Not.Null);

            var actual = await _dfs.DagApi.GetAsync<Name>(id);
            Assert.That(actual, Is.Not.Null);
            Assert.That(expected.First, Is.EqualTo(actual.First));
            Assert.That(expected.Last, Is.EqualTo(actual.Last));

            var value = (string) await _dfs.DagApi.GetAsync(id.Encode() + "/Last");
            Assert.That(expected.Last, Is.EqualTo(value));
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
            Assert.That(id, Is.Not.Null);
            Assert.That(id.Encoding, Is.EqualTo("base32"));
            Assert.That(id.Version, Is.EqualTo(1));

            var actual = await _dfs.DagApi.GetAsync<Name>(id);
            Assert.That(actual, Is.Not.Null);
            Assert.That(expected.First, Is.EqualTo(actual.First));
            Assert.That(expected.Last, Is.EqualTo(actual.Last));

            var value = (string) await _dfs.DagApi.GetAsync(id.Encode() + "/Last");
            Assert.That(expected.Last, Is.EqualTo(value));
        }

        [Test]
        public async Task PutAndGet_POCO()
        {
            var expected = new Name {First = "John", Last = "Smith"};
            var id = await _dfs.DagApi.PutAsync(expected);
            Assert.That(id, Is.Not.Null);

            var actual = await _dfs.DagApi.GetAsync<Name>(id);
            Assert.That(actual, Is.Not.Null);
            Assert.That(expected.First, Is.EqualTo(actual.First));
            Assert.That(expected.Last, Is.EqualTo(actual.Last));

            var value = (string) await _dfs.DagApi.GetAsync(id.Encode() + "/Last");
            Assert.That(expected.Last, Is.EqualTo(value));
        }

        [Test]
        public async Task Get_Raw2()
        {
            var data = Encoding.UTF8.GetBytes("abc");
            var id = await _dfs.BlockApi.PutAsync(data, "raw");
            Assert.That(id.Encode(), Is.EqualTo("bafkreif2pall7dybz7vecqka3zo24irdwabwdi4wc55jznaq75q7eaavvu"));

            var actual = await _dfs.DagApi.GetAsync(id);
            Assert.That(Convert.ToBase64String(data), Is.EqualTo((string) actual["data"]));
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
        //     Assert.That((string) expected, Is.EqualTo((string) cid));
        // }
    }
}
