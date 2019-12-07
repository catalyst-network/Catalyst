using System;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.Configuration.Annotations;
using Catalyst.Abstractions.Dfs;
using Lib.P2P;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class DagApiTest
    {
        byte[] blob = Encoding.UTF8.GetBytes("blorb");
        string blob64 = "YmxvcmI"; // base 64 encoded with no padding
        private IDfs ipfs;

        public DagApiTest(ITestOutputHelper output)
        {
            ipfs = new TestFixture(output).Ipfs;      
        }

        [Fact]
        public async Task Get_Raw()
        {
            var cid = await ipfs.Block.PutAsync(blob, contentType: "raw");
            Assert.Equal("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string) cid);

            var dag = await ipfs.Dag.GetAsync(cid);
            Assert.Equal(blob64, (string) dag["data"]);
        }

        class Name
        {
            public string First { get; set; }
            public string Last { get; set; }
        }

        class name
        {
            public string first { get; set; }
            public string last { get; set; }
        }

        [Fact]
        public async Task PutAndGet_JSON()
        {
            var expected = new JObject();
            expected["a"] = "alpha";
            var expectedId = "bafyreigdhej736dobd6z3jt2vxsxvbwrwgyts7e7wms6yrr46rp72uh5bu";
            var id = await ipfs.Dag.PutAsync(expected);
            Assert.NotNull(id);
            Assert.Equal(expectedId, (string) id);

            var actual = await ipfs.Dag.GetAsync(id);
            Assert.NotNull(actual);
            Assert.Equal(expected["a"], actual["a"]);

            var value = (string) await ipfs.Dag.GetAsync(expectedId + "/a");
            Assert.Equal(expected["a"], value);
        }

        [Fact]
        public async Task PutAndGet_poco()
        {
            var expected = new name {first = "John", last = "Smith"};
            var id = await ipfs.Dag.PutAsync(expected);
            Assert.NotNull(id);

            var actual = await ipfs.Dag.GetAsync<name>(id);
            Assert.NotNull(actual);
            Assert.Equal(expected.first, actual.first);
            Assert.Equal(expected.last, actual.last);

            var value = (string) await ipfs.Dag.GetAsync(id.Encode() + "/last");
            Assert.Equal(expected.last, value);
        }

        [Fact]
        public async Task PutAndGet_poco_CidEncoding()
        {
            var expected = new name {first = "John", last = "Smith"};
            var id = await ipfs.Dag.PutAsync(expected, encoding: "base32");
            Assert.NotNull(id);
            Assert.Equal("base32", id.Encoding);
            Assert.Equal(1, id.Version);

            var actual = await ipfs.Dag.GetAsync<name>(id);
            Assert.NotNull(actual);
            Assert.Equal(expected.first, actual.first);
            Assert.Equal(expected.last, actual.last);

            var value = (string) await ipfs.Dag.GetAsync(id.Encode() + "/last");
            Assert.Equal(expected.last, value);
        }

        [Fact]
        public async Task PutAndGet_POCO()
        {
            var expected = new Name {First = "John", Last = "Smith"};
            var id = await ipfs.Dag.PutAsync(expected);
            Assert.NotNull(id);

            var actual = await ipfs.Dag.GetAsync<Name>(id);
            Assert.NotNull(actual);
            Assert.Equal(expected.First, actual.First);
            Assert.Equal(expected.Last, actual.Last);

            var value = (string) await ipfs.Dag.GetAsync(id.Encode() + "/Last");
            Assert.Equal(expected.Last, value);
        }

        [Fact]
        public async Task Get_Raw2()
        {
            var data = Encoding.UTF8.GetBytes("abc");
            var id = await ipfs.Block.PutAsync(data, "raw");
            Assert.Equal("bafkreif2pall7dybz7vecqka3zo24irdwabwdi4wc55jznaq75q7eaavvu", id.Encode());

            var actual = await ipfs.Dag.GetAsync(id);
            Assert.Equal(Convert.ToBase64String(data), (string) actual["data"]);
        }

        // // https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/DAG.md
        // [Fact]
        // [Ignore("https://github.com/richardschneider/net-ipfs-engine/issues/30")]
        // public async Task Example1()
        // {
        //     Cid expected =
        //         "zBwWX9ecx5F4X54WAjmFLErnBT6ByfNxStr5ovowTL7AhaUR98RWvXPS1V3HqV1qs3r5Ec5ocv7eCdbqYQREXNUfYNuKG";
        //     var obj = new {simple = "object"};
        //     var cid = await ipfs.Dag.PutAsync(obj, multiHash: "sha3-512");
        //     Assert.Equal((string) expected, (string) cid);
        // }
    }
}
