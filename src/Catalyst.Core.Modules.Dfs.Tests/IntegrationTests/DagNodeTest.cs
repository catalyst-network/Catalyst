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
            Assert.That(0, Is.EqualTo(node.DataBytes.Length));
            Assert.That(0, Is.EqualTo(node.Links.Count()));
            Assert.That(0, Is.EqualTo(node.Size));
            Assert.That("QmdfTbBqBPQ7VNxZEYEj14VmRuZBkqFbiwReogJgS1zR1n", Is.EqualTo(node.Id.ToString()));

            RoundtripTest(node);
        }

        [Test]
        public void DataOnlyDag()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc);
            
            Assert.That(abc, Is.EqualTo(node.DataBytes));
            Assert.That(0, Is.EqualTo(node.Links.Count()));
            Assert.That("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", Is.EqualTo(node.Id.ToString()));
            Assert.That(5, Is.EqualTo(node.Size));

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
            Assert.That(0, Is.EqualTo(node.DataBytes.Length));
            Assert.That(1, Is.EqualTo(node.Links.Count()));
            Assert.That("QmVdMJFGTqF2ghySAmivGiQvsr9ZH7ujnNGBkLNNCe4HUE", Is.EqualTo(node.Id.ToString()));
            Assert.That(43, Is.EqualTo(node.Size));

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
            Assert.That(0, Is.EqualTo(node.DataBytes.Length));
            Assert.That(2, Is.EqualTo(node.Links.Count()));
            Assert.That("QmbNgNPPykP4YTuAeSa3DsnBJWLVxccrqLUZDPNQfizGKs", Is.EqualTo(node.Id.ToString()));

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
            
            Assert.That(ab, Is.EqualTo(node.DataBytes));
            Assert.That(2, Is.EqualTo(node.Links.Count()));
            Assert.That("Qma5sYpEc9hSYdkuXpMDJYem95Mj7hbEd9C412dEQ4ZkfP", Is.EqualTo(node.Id.ToString()));

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
            Assert.That(ab, Is.EqualTo(node.DataBytes));
            Assert.That(2, Is.EqualTo(node.Links.Count()));
            Assert.That("Qma5sYpEc9hSYdkuXpMDJYem95Mj7hbEd9C412dEQ4ZkfP", Is.EqualTo(node.Id.ToString()));
        }

        [Test]
        public void HashingAlgorithmTest()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc);
            Assert.That(abc, Is.EqualTo(node.DataBytes));
            Assert.That(0, Is.EqualTo(node.Links.Count()));
            Assert.That("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", Is.EqualTo(node.Id.ToString()));
            Assert.That(5, Is.EqualTo(node.Size));

            RoundtripTest(node);
        }

        [Test]
        public void ToLink()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc);
            var link = node.ToLink();
            Assert.That("", Is.EqualTo(link.Name));
            Assert.That(node.Id, Is.EqualTo(link.Id));
            Assert.That(node.Size, Is.EqualTo(link.Size));
        }

        [Test]
        public void ToLinkWithName()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc);
            var link = node.ToLink("abc");
            Assert.That("abc", Is.EqualTo(link.Name));
            Assert.That(node.Id.ToString(), Is.EqualTo(link.Id.ToString()));
            Assert.That(node.Size, Is.EqualTo(link.Size));
        }

        [Test]
        public void AddLink()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a);

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b);

            var cnode = bnode.AddLink(anode.ToLink());
            Assert.That(ReferenceEquals(bnode, cnode), Is.False);
            Assert.That(1, Is.EqualTo(cnode.DataBytes.Length));
            Assert.That(1, Is.EqualTo(cnode.Links.Count()));
            Assert.That(anode.Id.ToString(), Is.EqualTo(cnode.Links.First().Id.ToString()));
            Assert.That(anode.Size, Is.EqualTo(cnode.Links.First().Size));

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
            Assert.That(ReferenceEquals(dnode, cnode), Is.False);
            Assert.That(1, Is.EqualTo(dnode.DataBytes.Length));
            Assert.That(1, Is.EqualTo(dnode.Links.Count()));
            Assert.That(bnode.Id.ToString(), Is.EqualTo(dnode.Links.First().Id.ToString()));
            Assert.That(bnode.Size, Is.EqualTo(dnode.Links.First().Size));

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
            Assert.That("0801", Is.EqualTo(node.DataBytes.ToHexString()));
            Assert.That(1, Is.EqualTo(node.Links.Count()));
            var link = node.Links.First();
            Assert.That("hello", Is.EqualTo(link.Name));
            Assert.That(1, Is.EqualTo(link.Id.Version));
            Assert.That("raw", Is.EqualTo(link.Id.ContentType));
            Assert.That("sha2-512", Is.EqualTo(link.Id.Hash.Algorithm.Name));
            Assert.That(11, Is.EqualTo(link.Size));
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
            Assert.That(a.DataBytes.Length, Is.EqualTo(b.DataBytes.Length));
            Assert.That(a.Links.Count(), Is.EqualTo(b.Links.Count()));
            Assert.That(a.Size, Is.EqualTo(b.Size));
            Assert.That(a.Id, Is.Not.EqualTo(b.Id));

            RoundtripTest(b);
        }

        private static void RoundtripTest(IDagNode a)
        {
            var ms = new MemoryStream();
            a.Write(ms);
            ms.Position = 0;
            var b = new DagNode(ms);
            
            Assert.That(a.DataBytes, Is.EqualTo(b.DataBytes));
            Assert.That(a.ToArray(), Is.EqualTo(b.ToArray()));
            Assert.That(a.Links.Count(), Is.EqualTo(b.Links.Count()));
            foreach (var _ in a.Links.Zip(b.Links, (first, second) =>
            {
                Assert.That(first.Id, Is.EqualTo(second.Id));
                Assert.That(first.Name, Is.EqualTo(second.Name));
                Assert.That(first.Size, Is.EqualTo(second.Size));
                return first;
            }).ToArray())

                using (var first = a.DataStream)
                {
                    using (var second = b.DataStream)
                    {
                        Assert.That(first.Length, Is.EqualTo(second.Length));
                        for (var i = 0; i < first.Length; ++i)
                        {
                            Assert.That(first.ReadByte(), Is.EqualTo(second.ReadByte()));
                        }
                    }
                }
        }
    }
}

