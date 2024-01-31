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

namespace Catalyst.KBucket
{
    public class DistanceTest
    {
        /// <summary>
        ///  From https://github.com/tristanls/k-bucket/blob/master/test/defaultDistance.js
        /// </summary>
        [Test]
        public void Tristanls()
        {
            var bucket = new KBucket<Contact>();

            Assert.That((ulong) 0, Is.EqualTo((ulong)bucket.Distance(new byte[] {0x00}, new byte[] {0x00})));
            Assert.That((ulong) 1, Is.EqualTo((ulong)bucket.Distance(new byte[] {0x00}, new byte[] {0x01})));
            Assert.That((ulong) 3, Is.EqualTo((ulong)bucket.Distance(new byte[] {0x02}, new byte[] {0x01})));
            Assert.That((ulong) 255, Is.EqualTo((ulong)bucket.Distance(new byte[] {0x00}, new byte[] {0x00, 0x00})));
            Assert.That((ulong) 16640, Is.EqualTo((ulong)bucket.Distance(new byte[] {0x01, 0x24}, new byte[] {0x40, 0x24})));
        }

        [Test]
        public void ByContact()
        {
            var bucket = new KBucket<Contact>();
            var c0 = new Contact((byte) 0);
            var c1 = new Contact((byte) 1);
            var c2 = new Contact((byte) 2);
            var c00 = new Contact((byte) 0, (byte) 0);
            var c0124 = new Contact((byte) 0x01, (byte) 0x24);
            var c4024 = new Contact((byte) 0x40, (byte) 0x24);
            Assert.That((ulong) 0, Is.EqualTo((ulong)bucket.Distance(c0, c0)));
            Assert.That((ulong) 1, Is.EqualTo((ulong)bucket.Distance(c0, c1)));
            Assert.That((ulong) 3, Is.EqualTo((ulong)bucket.Distance(c2, c1)));
            Assert.That((ulong) 255, Is.EqualTo((ulong)bucket.Distance(c0, c00)));
            Assert.That((ulong) 16640, Is.EqualTo((ulong)bucket.Distance(c0124, c4024)));
        }
    }
}
