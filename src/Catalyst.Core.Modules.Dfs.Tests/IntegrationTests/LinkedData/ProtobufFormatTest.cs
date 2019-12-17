using System.Text;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Dfs.LinkedData;
using Catalyst.Core.Modules.Hashing;
using MultiFormats.Registry;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.LinkedData
{
    public class ProtobufFormatTest
    {
        private readonly IHashProvider _hashProvider;
        ILinkedDataFormat formatter = new ProtobufFormat();

        public ProtobufFormatTest()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
        }

        [Fact]
        public void Empty()
        {
            var data = new byte[0];
            var node = new DagNode(data, _hashProvider);

            var cbor = formatter.Deserialise(node.ToArray());
            Assert.Equal(data, cbor["data"].GetByteString());
            Assert.Equal(0, cbor["links"].Values.Count);

            var node1 = formatter.Serialize(cbor);
            Assert.Equal(node.ToArray(), node1);
        }

        [Fact]
        public void DataOnly()
        {
            var data = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(data, _hashProvider);

            var cbor = formatter.Deserialise(node.ToArray());
            Assert.Equal(data, cbor["data"].GetByteString());
            Assert.Equal(0, cbor["links"].Values.Count);

            var node1 = formatter.Serialize(cbor);
            Assert.Equal(node.ToArray(), node1);
        }

        [Fact]
        public void LinksOnly()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a, _hashProvider);
            var alink = anode.ToLink("a");

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b, _hashProvider);
            var blink = bnode.ToLink();

            var node = new DagNode(null, _hashProvider, new[] {alink, blink});
            var cbor = formatter.Deserialise(node.ToArray());

            Assert.Equal(2, cbor["links"].Values.Count);

            var link = cbor["links"][0];
            Assert.Equal("bafykbzacedvmf73p2ubr3ldsavqwiwlx753yttzczdqs5ddua46unou7knkou", link["Cid"]["/"].AsString());
            Assert.Equal("", link["Name"].AsString());
            Assert.Equal(3, link["Size"].AsInt32());

            link = cbor["links"][1];
            Assert.Equal("bafykbzaceaulxu25d3hnsm6wphzsgb5hi44hksw346dw7whrhgcw4k7p6lqaw", link["Cid"]["/"].AsString());
            Assert.Equal("a", link["Name"].AsString());
            Assert.Equal(3, link["Size"].AsInt32());

            var node1 = formatter.Serialize(cbor);
            Assert.Equal(node.ToArray(), node1);
        }
    }
}
