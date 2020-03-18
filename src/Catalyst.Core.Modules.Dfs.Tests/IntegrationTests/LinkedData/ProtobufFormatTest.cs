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

using System.Text;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Dfs.LinkedData;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.LinkedData
{
    public class ProtobufFormatTest
    {
        private ILinkedDataFormat formatter = new ProtobufFormat();

        [Fact]
        public void Empty()
        {
            var data = new byte[0];
            var node = new DagNode(data);

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
            var node = new DagNode(data);

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
            var anode = new DagNode(a);
            var alink = anode.ToLink("a");

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b);
            var blink = bnode.ToLink();

            var node = new DagNode(null, new[] {alink, blink});
            var cbor = formatter.Deserialise(node.ToArray());

            Assert.Equal(2, cbor["links"].Values.Count);

            var link = cbor["links"][0];
            Assert.Equal("QmYpoNmG5SWACYfXsDztDNHs29WiJdmP7yfcMd7oVa75Qv", link["Cid"]["/"].AsString());
            Assert.Equal("", link["Name"].AsString());
            Assert.Equal(3, link["Size"].AsInt32());

            link = cbor["links"][1];
            Assert.Equal("QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", link["Cid"]["/"].AsString());
            Assert.Equal("a", link["Name"].AsString());
            Assert.Equal(3, link["Size"].AsInt32());

            var node1 = formatter.Serialize(cbor);
            Assert.Equal(node.ToArray(), node1);
        }
    }
}
