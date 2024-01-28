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

using System.Linq;

namespace Catalyst.KBucket
{
    /// <summary>
    ///   From https://github.com/tristanls/k-bucket/blob/master/test/closest.js
    /// </summary>
    public class ClosestTest
    {
        [Test]
        public void ClosestNodes()
        {
            var kBucket = new KBucket<Contact>();
            for (var i = 0; i < 0x12; ++i) kBucket.Add(new Contact((byte) i));

            var contact = new Contact((byte) 0x15); // 00010101
            var contacts = kBucket.Closest(contact).Take(3).ToArray();
            Assert.That(new byte[]
            {
                0x11
            }, Is.EquivalentTo(contacts[0].Id)); // distance: 00000100
            Assert.That(new byte[]
            {
                0x10
            }, Is.EquivalentTo(contacts[1].Id)); // distance: 00000101
            Assert.That(new byte[]
            {
                0x05
            }, Is.EquivalentTo(contacts[2].Id)); // distance: 00010000
        }

        [Test]
        public void All()
        {
            var kBucket = new KBucket<Contact>
            {
                LocalContactId = new byte[] {0, 0}
            };
            for (var i = 0; i < 1000; ++i) kBucket.Add(new Contact((byte) (i / 256), (byte) (i % 256)));

            var contact = new Contact((byte) 0x80, (byte) 0x80);
            var contacts = kBucket.Closest(contact);
            Assert.That(contacts.Count() > 100, Is.True);
        }

        [Test]
        public void ClosestNodes_ExactMatch()
        {
            var kBucket = new KBucket<Contact>();
            for (var i = 0; i < 0x12; ++i) kBucket.Add(new Contact((byte) i));

            var contact = new Contact((byte) 0x11); // 00010001
            var contacts = kBucket.Closest(contact).Take(3).ToArray();
            Assert.That(new byte[] {0x11}, Is.EquivalentTo(contacts[0].Id)); // distance: 00000000
            Assert.That(new byte[] {0x10}, Is.EquivalentTo(contacts[1].Id)); // distance: 00000001
            Assert.That(new byte[] {0x01}, Is.EquivalentTo(contacts[2].Id)); // distance: 00010000
        }

        [Test]
        public void ClosestNodes_PartialBuckets()
        {
            var kBucket = new KBucket<Contact>
            {
                LocalContactId = new byte[] {0, 0}
            };
            for (var i = 0; i < kBucket.ContactsPerBucket; ++i)
            {
                kBucket.Add(new Contact((byte) 0x80, (byte) i));
                kBucket.Add(new Contact((byte) 0x01, (byte) i));
            }

            kBucket.Add(new Contact((byte) 0x00, (byte) 0x01));

            var contact = new Contact((byte) 0x00, (byte) 0x03);
            var contacts = kBucket.Closest(contact).Take(22).ToArray();

            Assert.That(contacts[0].Id, Is.EquivalentTo(new byte[] {0x00, 0x01})); // distance: 0000000000000010
            Assert.That(contacts[1].Id, Is.EquivalentTo(new byte[] {0x01, 0x03})); // distance: 0000000100000000
            Assert.That(contacts[2].Id, Is.EquivalentTo(new byte[] {0x01, 0x02})); // distance: 0000000100000010
            Assert.That(contacts[3].Id, Is.EquivalentTo(new byte[] {0x01, 0x01}));
            Assert.That(contacts[4].Id, Is.EquivalentTo(new byte[] {0x01, 0x00}));
            Assert.That(contacts[5].Id, Is.EquivalentTo(new byte[] {0x01, 0x07}));
            Assert.That(contacts[6].Id, Is.EquivalentTo(new byte[] {0x01, 0x06}));
            Assert.That(contacts[7].Id, Is.EquivalentTo(new byte[] {0x01, 0x05}));
            Assert.That(contacts[8].Id, Is.EquivalentTo(new byte[] {0x01, 0x04}));
            Assert.That(contacts[9].Id, Is.EquivalentTo(new byte[] {0x01, 0x0b}));
            Assert.That(contacts[10].Id, Is.EquivalentTo(new byte[] {0x01, 0x0a}));
            Assert.That(contacts[11].Id, Is.EquivalentTo(new byte[] {0x01, 0x09}));
            Assert.That(contacts[12].Id, Is.EquivalentTo(new byte[] {0x01, 0x08}));
            Assert.That(contacts[13].Id, Is.EquivalentTo(new byte[] {0x01, 0x0f}));
            Assert.That(contacts[14].Id, Is.EquivalentTo(new byte[] {0x01, 0x0e}));
            Assert.That(contacts[15].Id, Is.EquivalentTo(new byte[] {0x01, 0x0d}));
            Assert.That(contacts[16].Id, Is.EquivalentTo(new byte[] {0x01, 0x0c}));
            Assert.That(contacts[17].Id, Is.EquivalentTo(new byte[] {0x01, 0x13}));
            Assert.That(contacts[18].Id, Is.EquivalentTo(new byte[] {0x01, 0x12}));
            Assert.That(contacts[19].Id, Is.EquivalentTo(new byte[] {0x01, 0x11}));
            Assert.That(contacts[20].Id, Is.EquivalentTo(new byte[] {0x01, 0x10}));
            Assert.That(contacts[21].Id, Is.EquivalentTo(new byte[] {0x80, 0x03}));
        }
    }
}
