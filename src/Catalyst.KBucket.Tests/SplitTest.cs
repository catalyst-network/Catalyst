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

namespace Catalyst.KBucket
{
    /// <summary>
    ///   From https://github.com/tristanls/k-bucket/blob/master/test/split.js
    /// </summary>
    public class SplitTest
    {
        [Test]
        public void OneContactDoesNotSplit()
        {
            var kBucket = new KBucket<Contact>
            {
                new Contact("a")
            };
            Assert.That(kBucket.Root.Left, Is.Null);
            Assert.That(kBucket.Root.Right, Is.Null);
            Assert.That(kBucket.Root.Contacts, Is.Not.Null);
        }

        [Test]
        public void MaxContactsPerNodeDoesNotSplit()
        {
            var kBucket = new KBucket<Contact>();
            for (var i = 0; i < kBucket.ContactsPerBucket; ++i) kBucket.Add(new Contact(i));

            Assert.That(kBucket.Root.Left, Is.Null);
            Assert.That(kBucket.Root.Right, Is.Null);
            Assert.That(kBucket.Root.Contacts, Is.Not.Null);
        }

        [Test]
        public void MaxContactsPerNodePlusOneDoetSplit()
        {
            var kBucket = new KBucket<Contact>();
            for (var i = 0; i < kBucket.ContactsPerBucket + 1; ++i) kBucket.Add(new Contact(i));

            Assert.That(kBucket.Root.Left, Is.Not.Null);
            Assert.That(kBucket.Root.Right, Is.Not.Null);
            Assert.That(kBucket.Root.Contacts, Is.Null);
        }

        [Test]
        public void SplitNodesContainsAllContacts()
        {
            var kBucket = new KBucket<Contact>
            {
                LocalContactId = new byte[] {0x00}
            };
            var contacts = new List<Contact>();
            for (var i = 0; i < kBucket.ContactsPerBucket + 1; ++i)
            {
                var contact = new Contact((byte) i);
                contacts.Add(contact);
                kBucket.Add(contact);
            }

            foreach (var contact in contacts) Assert.That(kBucket.Contains(contact), Is.True);
        }

        [Test]
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
                Assert.Equals(dontSplit, node.DontSplit);
            }
        }

        [Test]
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
            Assert.That(0, Is.Not.EqualTo(pings));
        }
    }
}
