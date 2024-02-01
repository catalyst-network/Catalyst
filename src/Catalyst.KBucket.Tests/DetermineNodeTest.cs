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
    /// <summary>
    ///   From https://github.com/tristanls/k-bucket/blob/master/test/determineNode.js
    /// </summary>
    public class DetermineNodeTest
    {
        private static readonly Bucket<Contact> Left = new Bucket<Contact>();
        private static readonly Bucket<Contact> Right = new Bucket<Contact>();
        private static readonly Bucket<Contact> Root = new Bucket<Contact> {Left = Left, Right = Right};

        [Test]
        public void Tests()
        {
            var kBucket = new KBucket<Contact>();
            Bucket<Contact> actual;

            actual = kBucket._DetermineNode(Root, new byte[] {0x00}, 0);
            Assert.That(Left, Is.EqualTo(actual));

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 0);
            Assert.That(Left, Is.EqualTo(actual));

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 1);
            Assert.That(Right, Is.EqualTo(actual));

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 2);
            Assert.That(Left, Is.EqualTo(actual));

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 9);
            Assert.That(Left, Is.EqualTo(actual));

            actual = kBucket._DetermineNode(Root, new byte[] {0x41}, 7);
            Assert.That(Right, Is.EqualTo(actual));

            actual = kBucket._DetermineNode(Root, new byte[] {0x41, 0x00}, 7);
            Assert.That(Right, Is.EqualTo(actual));

            actual = kBucket._DetermineNode(Root, new byte[] {0x00, 0x41, 0x00}, 15);
            Assert.That(Right, Is.EqualTo(actual));
        }
    }
}
