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

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catalyst.KBucket
{
    /// <summary>
    ///   From https://github.com/tristanls/k-bucket/blob/master/test/split.js
    /// </summary>
    [TestClass]
    public class SplitTest
    {
        [TestMethod]
        public void OneContactDoesNotSplit()
        {
            var kBucket = new KBucket<Contact>
            {
                new Contact("a")
            };
            Assert.IsNull(kBucket.Root.Left);
            Assert.IsNull(kBucket.Root.Right);
            Assert.IsNotNull(kBucket.Root.Contacts);
        }

        [TestMethod]
        public void MaxContactsPerNodeDoesNotSplit()
        {
            KBucket<Contact> kBucket = new();
            for (var i = 0; i < kBucket.ContactsPerBucket; ++i) kBucket.Add(new Contact(i));

            Assert.IsNull(kBucket.Root.Left);
            Assert.IsNull(kBucket.Root.Right);
            Assert.IsNotNull(kBucket.Root.Contacts);
        }

        [TestMethod]
        public void MaxContactsPerNodePlusOneDoetSplit()
        {
            KBucket<Contact> kBucket = new();
            for (var i = 0; i < kBucket.ContactsPerBucket + 1; ++i) kBucket.Add(new Contact(i));

            Assert.IsNotNull(kBucket.Root.Left);
            Assert.IsNotNull(kBucket.Root.Right);
            Assert.IsNull(kBucket.Root.Contacts);
        }

        [TestMethod]
        public void SplitNodesContainsAllContacts()
        {
            var kBucket = new KBucket<Contact>
            {
                LocalContactId = new byte[] {0x00}
            };
            List<Contact> contacts = new();
            for (var i = 0; i < kBucket.ContactsPerBucket + 1; ++i)
            {
                Contact contact = new((byte) i);
                contacts.Add(contact);
                kBucket.Add(contact);
            }

            foreach (var contact in contacts) Assert.IsTrue(kBucket.Contains(contact));
        }

        [TestMethod]
        public void FarAway()
        {
            var kBucket = new KBucket<Contact>
            {
                LocalContactId = new byte[] {0x00}
            };
            for (var i = 0; i < kBucket.ContactsPerBucket + 1; ++i) kBucket.Add(new Contact((byte) i));

            // since localNodeId is 0x00, we expect every right node to be "far" and
            // therefore marked as "dontSplit = true"
            // there will be one "left" node and four "right" nodes (t.expect(5)) 
            Traverse(kBucket.Root, false);
        }

        private void Traverse(Bucket<Contact> node, bool dontSplit)
        {
            if (node.Contacts == null)
            {
                Traverse(node.Left, false);
                Traverse(node.Right, true);
            }
            else
            {
                Assert.AreEqual(dontSplit, node.DontSplit);
            }
        }

        [TestMethod]
        public void PingEvent()
        {
            var kBucket = new KBucket<Contact>
            {
                LocalContactId = new byte[] {0x00}
            };
            var pings = 0;
            kBucket.Ping += (s, e) =>
            {
                kBucket.Remove(e.Oldest.First());
                kBucket.Add(e.Newest);
                ++pings;
            };

            for (var i = 0; i < 0x255; ++i) kBucket.Add(new Contact((byte) i));
            Assert.AreNotEqual(0, pings);
        }
    }
}
