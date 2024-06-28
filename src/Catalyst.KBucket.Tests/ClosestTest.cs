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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catalyst.KBucket
{
    /// <summary>
    ///   From https://github.com/tristanls/k-bucket/blob/master/test/closest.js
    /// </summary>
    [TestClass]
    public class ClosestTest
    {
        [TestMethod]
        public void ClosestNodes()
        {
            var kBucket = new KBucket<Contact>();
            for (var i = 0; i < 0x12; ++i) kBucket.Add(new Contact((byte) i));

            var contact = new Contact((byte) 0x15); // 00010101
            var contacts = kBucket.Closest(contact).Take(3).ToArray();
            CollectionAssert.AreEqual(new byte[]
            {
                0x11
            }, contacts[0].Id); // distance: 00000100
            CollectionAssert.AreEqual(new byte[]
            {
                0x10
            }, contacts[1].Id); // distance: 00000101
            CollectionAssert.AreEqual(new byte[]
            {
                0x05
            }, contacts[2].Id); // distance: 00010000
        }

        [TestMethod]
        public void All()
        {
            var kBucket = new KBucket<Contact>
            {
                LocalContactId = new byte[] {0, 0}
            };
            for (var i = 0; i < 1000; ++i) kBucket.Add(new Contact((byte) (i / 256), (byte) (i % 256)));

            var contact = new Contact((byte) 0x80, (byte) 0x80);
            var contacts = kBucket.Closest(contact);
            Assert.IsTrue(contacts.Count() > 100);
        }

        [TestMethod]
        public void ClosestNodes_ExactMatch()
        {
            var kBucket = new KBucket<Contact>();
            for (var i = 0; i < 0x12; ++i) kBucket.Add(new Contact((byte) i));

            var contact = new Contact((byte) 0x11); // 00010001
            var contacts = kBucket.Closest(contact).Take(3).ToArray();
            CollectionAssert.AreEqual(new byte[] {0x11}, contacts[0].Id); // distance: 00000000
            CollectionAssert.AreEqual(new byte[] {0x10}, contacts[1].Id); // distance: 00000001
            CollectionAssert.AreEqual(new byte[] {0x01}, contacts[2].Id); // distance: 00010000
        }

        [TestMethod]
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

            CollectionAssert.AreEqual(contacts[0].Id, new byte[] {0x00, 0x01}); // distance: 0000000000000010
            CollectionAssert.AreEqual(contacts[1].Id, new byte[] {0x01, 0x03}); // distance: 0000000100000000
            CollectionAssert.AreEqual(contacts[2].Id, new byte[] {0x01, 0x02}); // distance: 0000000100000010
            CollectionAssert.AreEqual(contacts[3].Id, new byte[] {0x01, 0x01});
            CollectionAssert.AreEqual(contacts[4].Id, new byte[] {0x01, 0x00});
            CollectionAssert.AreEqual(contacts[5].Id, new byte[] {0x01, 0x07});
            CollectionAssert.AreEqual(contacts[6].Id, new byte[] {0x01, 0x06});
            CollectionAssert.AreEqual(contacts[7].Id, new byte[] {0x01, 0x05});
            CollectionAssert.AreEqual(contacts[8].Id, new byte[] {0x01, 0x04});
            CollectionAssert.AreEqual(contacts[9].Id, new byte[] {0x01, 0x0b});
            CollectionAssert.AreEqual(contacts[10].Id, new byte[] {0x01, 0x0a});
            CollectionAssert.AreEqual(contacts[11].Id, new byte[] {0x01, 0x09});
            CollectionAssert.AreEqual(contacts[12].Id, new byte[] {0x01, 0x08});
            CollectionAssert.AreEqual(contacts[13].Id, new byte[] {0x01, 0x0f});
            CollectionAssert.AreEqual(contacts[14].Id, new byte[] {0x01, 0x0e});
            CollectionAssert.AreEqual(contacts[15].Id, new byte[] {0x01, 0x0d});
            CollectionAssert.AreEqual(contacts[16].Id, new byte[] {0x01, 0x0c});
            CollectionAssert.AreEqual(contacts[17].Id, new byte[] {0x01, 0x13});
            CollectionAssert.AreEqual(contacts[18].Id, new byte[] {0x01, 0x12});
            CollectionAssert.AreEqual(contacts[19].Id, new byte[] {0x01, 0x11});
            CollectionAssert.AreEqual(contacts[20].Id, new byte[] {0x01, 0x10});
            CollectionAssert.AreEqual(contacts[21].Id, new byte[] {0x80, 0x03});
        }
    }
}
