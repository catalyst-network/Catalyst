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
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.LinkedData
{
    public class ProtobufFormatTest
    {
        private ILinkedDataFormat formatter = new ProtobufFormat();

        [Test]
        public void Empty()
        {
            var data = new byte[0];
            var node = new DagNode(data);

            var cbor = formatter.Deserialise(node.ToArray());
            Assert.That(data, Is.EqualTo(cbor["data"].GetByteString()));
            Assert.That(cbor["links"].Values.Count, Is.EqualTo(0));

            var node1 = formatter.Serialize(cbor);
            Assert.That(node.ToArray(), Is.EqualTo(node1));
        }

        [Test]
        public void DataOnly()
        {
            var data = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(data);

            var cbor = formatter.Deserialise(node.ToArray());
            Assert.That(data, Is.EqualTo(cbor["data"].GetByteString()));
            Assert.That(cbor["links"].Values.Count, Is.EqualTo(0));

            var node1 = formatter.Serialize(cbor);
            Assert.That(node.ToArray(), Is.EqualTo(node1));
        }

        [Test]
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

            Assert.That(cbor["links"].Values.Count, Is.EqualTo(2));

            var link = cbor["links"][0];
            Assert.That(link["Cid"]["/"].AsString(), Is.EqualTo("QmYpoNmG5SWACYfXsDztDNHs29WiJdmP7yfcMd7oVa75Qv"));
            Assert.That(link["Name"].AsString(), Is.EqualTo(""));
            Assert.That(link["Size"].AsInt32(), Is.EqualTo(3));

            link = cbor["links"][1];
            Assert.That(link["Cid"]["/"].AsString(), Is.EqualTo("QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi"));
            Assert.That(link["Name"].AsString(), Is.EqualTo("a"));
            Assert.That(link["Size"].AsInt32(), Is.EqualTo(3));

            var node1 = formatter.Serialize(cbor);
            Assert.That(node.ToArray(), Is.EqualTo(node1));
        }
    }
}
