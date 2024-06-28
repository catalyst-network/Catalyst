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

using System.IO;
using System.Linq;
using System.Text;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Hashing;
using Catalyst.TestUtils;
using Google.Protobuf;
using MultiFormats;
using MultiFormats.Registry;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public sealed class DagNodeTest
    {
        public DagNodeTest()
        {
            new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("sha2-256"));
        }

        [Test]
        public void EmptyDag()
        {
            var node = new DagNode((byte[]) null);
            Assert.AreEqual(0, node.DataBytes.Length);
            Assert.AreEqual(0, node.Links.Count());
            Assert.AreEqual(0, node.Size);
            Assert.AreEqual("QmdfTbBqBPQ7VNxZEYEj14VmRuZBkqFbiwReogJgS1zR1n", node.Id.ToString());

            RoundtripTest(node);
        }

        [Test]
        public void DataOnlyDag()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc);
            
            Assert.AreEqual(abc, node.DataBytes);
            Assert.AreEqual(0, node.Links.Count());
            Assert.AreEqual("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", node.Id.ToString());
            Assert.AreEqual(5, node.Size);

            RoundtripTest(node);
        }

        [Test]
        public void LinkOnlyDag()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a);
            var alink = anode.ToLink("a");

            var node = new DagNode(null, new[]
            {
                alink
            });
            Assert.AreEqual(0, node.DataBytes.Length);
            Assert.AreEqual(1, node.Links.Count());
            Assert.AreEqual("QmVdMJFGTqF2ghySAmivGiQvsr9ZH7ujnNGBkLNNCe4HUE", node.Id.ToString());
            Assert.AreEqual(43, node.Size);

            RoundtripTest(node);
        }

        [Test]
        public void MultipleLinksOnlyDag()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a);
            var alink = anode.ToLink("a");

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b);
            var blink = bnode.ToLink("b");

            var node = new DagNode(null, new[]
            {
                alink, blink
            });
            Assert.AreEqual(0, node.DataBytes.Length);
            Assert.AreEqual(2, node.Links.Count());
            Assert.AreEqual("QmbNgNPPykP4YTuAeSa3DsnBJWLVxccrqLUZDPNQfizGKs", node.Id.ToString());

            RoundtripTest(node);
        }

        [Test]
        public void MultipleLinksDataDag()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a);
            var alink = anode.ToLink("a");

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b);
            var blink = bnode.ToLink("b");

            var ab = Encoding.UTF8.GetBytes("ab");
            var node = new DagNode(ab, new[]
            {
                alink, blink
            });
            
            Assert.AreEqual(ab, node.DataBytes);
            Assert.AreEqual(2, node.Links.Count());
            Assert.AreEqual("Qma5sYpEc9hSYdkuXpMDJYem95Mj7hbEd9C412dEQ4ZkfP", node.Id.ToString());

            RoundtripTest(node);
        }

        [Test]
        public void LinksAreSorted()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a);
            var alink = anode.ToLink("a");

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b);
            var blink = bnode.ToLink("b");

            var ab = Encoding.UTF8.GetBytes("ab");
            var node = new DagNode(ab, new[]
            {
                blink, alink
            });
            Assert.AreEqual(ab, node.DataBytes);
            Assert.AreEqual(2, node.Links.Count());
            Assert.AreEqual("Qma5sYpEc9hSYdkuXpMDJYem95Mj7hbEd9C412dEQ4ZkfP", node.Id.ToString());
        }

        [Test]
        public void HashingAlgorithmTest()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc);
            Assert.AreEqual(abc, node.DataBytes);
            Assert.AreEqual(0, node.Links.Count());
            Assert.AreEqual("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", node.Id.ToString());
            Assert.AreEqual(5, node.Size);

            RoundtripTest(node);
        }

        [Test]
        public void ToLink()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc);
            var link = node.ToLink();
            Assert.AreEqual("", link.Name);
            Assert.AreEqual(node.Id, link.Id);
            Assert.AreEqual(node.Size, link.Size);
        }

        [Test]
        public void ToLinkWithName()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc);
            var link = node.ToLink("abc");
            Assert.AreEqual("abc", link.Name);
            Assert.AreEqual(node.Id.ToString(), link.Id.ToString());
            Assert.AreEqual(node.Size, link.Size);
        }

        [Test]
        public void AddLink()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a);

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b);

            var cnode = bnode.AddLink(anode.ToLink());
            Assert.False(ReferenceEquals(bnode, cnode));
            Assert.AreEqual(1, cnode.DataBytes.Length);
            Assert.AreEqual(1, cnode.Links.Count());
            Assert.AreEqual(anode.Id.ToString(), cnode.Links.First().Id.ToString());
            Assert.AreEqual(anode.Size, cnode.Links.First().Size);

            RoundtripTest(cnode);
        }

        [Test]
        public void RemoveLink()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a);

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b);

            var c = Encoding.UTF8.GetBytes("c");
            var cnode = new DagNode(c, new[]
            {
                anode.ToLink(), bnode.ToLink()
            });

            var dnode = cnode.RemoveLink(anode.ToLink());
            Assert.False(ReferenceEquals(dnode, cnode));
            Assert.AreEqual(1, dnode.DataBytes.Length);
            Assert.AreEqual(1, dnode.Links.Count());
            Assert.AreEqual(bnode.Id.ToString(), dnode.Links.First().Id.ToString());
            Assert.AreEqual(bnode.Size, dnode.Links.First().Size);

            RoundtripTest(cnode);
        }

        [Test]
        public void NullStream()
        {
            TestUtils.ExceptionAssert.Throws(() => new DagNode((CodedInputStream) null));
            TestUtils.ExceptionAssert.Throws(() => new DagNode((Stream) null));
        }

        [Test]
        public void LinkWithCidv1()
        {
            var data =
                "124F0A4401551340309ECC489C12D6EB4CC40F50C902F2B4D0ED77EE511A7C7A9BCD3CA86D4CD86F989DD35BC5FF499670DA34255B45B0CFD830E81F605DCF7DC5542E93AE9CD76F120568656C6C6F180B0A020801"
                   .ToHexBuffer();
            var ms = new MemoryStream(data, false);
            var node = new DagNode(ms);
            Assert.AreEqual("0801", node.DataBytes.ToHexString());
            Assert.AreEqual(1, node.Links.Count());
            var link = node.Links.First();
            Assert.AreEqual("hello", link.Name);
            Assert.AreEqual(1, link.Id.Version);
            Assert.AreEqual("raw", link.Id.ContentType);
            Assert.AreEqual("sha2-512", link.Id.Hash.Algorithm.Name);
            Assert.AreEqual(11, link.Size);
        }

        [Test]
        public void SettingId()
        {
            var a = new DagNode((byte[]) null);
            var b = new DagNode((byte[]) null)
            {
                // Wrong hash but allowed.
                Id = "QmdfTbBqBPQ7VNxZEYEj14VmRuZBkqFbiwReogJgS1zR1m"
            };
            Assert.AreEqual(a.DataBytes.Length, b.DataBytes.Length);
            Assert.AreEqual(a.Links.Count(), b.Links.Count());
            Assert.AreEqual(a.Size, b.Size);
            Assert.AreNotEqual(a.Id, b.Id);

            RoundtripTest(b);
        }

        private static void RoundtripTest(IDagNode a)
        {
            var ms = new MemoryStream();
            a.Write(ms);
            ms.Position = 0;
            var b = new DagNode(ms);
            
            Assert.AreEqual(a.DataBytes, b.DataBytes);
            Assert.AreEqual(a.ToArray(), b.ToArray());
            Assert.AreEqual(a.Links.Count(), b.Links.Count());
            foreach (var _ in a.Links.Zip(b.Links, (first, second) =>
            {
                Assert.AreEqual(first.Id, second.Id);
                Assert.AreEqual(first.Name, second.Name);
                Assert.AreEqual(first.Size, second.Size);
                return first;
            }).ToArray())

                using (var first = a.DataStream)
                {
                    using (var second = b.DataStream)
                    {
                        Assert.AreEqual(first.Length, second.Length);
                        for (var i = 0; i < first.Length; ++i)
                        {
                            Assert.AreEqual(first.ReadByte(), second.ReadByte());
                        }
                    }
                }
        }
    }
}

