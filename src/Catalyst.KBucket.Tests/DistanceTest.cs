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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catalyst.KBucket
{
    [TestClass]
    public class DistanceTest
    {
        /// <summary>
        ///  From https://github.com/tristanls/k-bucket/blob/master/test/defaultDistance.js
        /// </summary>
        [TestMethod]
        public void Tristanls()
        {
            KBucket<Contact> bucket = new();

            Assert.AreEqual((ulong) 0, bucket.Distance(new byte[] {0x00}, new byte[] {0x00}));
            Assert.AreEqual((ulong) 1, bucket.Distance(new byte[] {0x00}, new byte[] {0x01}));
            Assert.AreEqual((ulong) 3, bucket.Distance(new byte[] {0x02}, new byte[] {0x01}));
            Assert.AreEqual((ulong) 255, bucket.Distance(new byte[] {0x00}, new byte[] {0x00, 0x00}));
            Assert.AreEqual((ulong) 16640, bucket.Distance(new byte[] {0x01, 0x24}, new byte[] {0x40, 0x24}));
        }

        [TestMethod]
        public void ByContact()
        {
            KBucket<Contact> bucket = new();
            Contact c0 = new((byte) 0);
            Contact c1 = new((byte) 1);
            Contact c2 = new((byte) 2);
            Contact c00 = new((byte) 0, (byte) 0);
            Contact c0124 = new((byte) 0x01, (byte) 0x24);
            Contact c4024 = new((byte) 0x40, (byte) 0x24);
            Assert.AreEqual((ulong) 0, bucket.Distance(c0, c0));
            Assert.AreEqual((ulong) 1, bucket.Distance(c0, c1));
            Assert.AreEqual((ulong) 3, bucket.Distance(c2, c1));
            Assert.AreEqual((ulong) 255, bucket.Distance(c0, c00));
            Assert.AreEqual((ulong) 16640, bucket.Distance(c0124, c4024));
        }
    }
}
