#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
    /// <summary>
    ///   From https://github.com/tristanls/k-bucket/blob/master/test/determineNode.js
    /// </summary>
    [TestClass]
    public class DetermineNodeTest
    {
        private static readonly Bucket<Contact> Left = new();
        private static readonly Bucket<Contact> Right = new();
        private static readonly Bucket<Contact> Root = new Bucket<Contact> {Left = Left, Right = Right};

        [TestMethod]
        public void Tests()
        {
            KBucket<Contact> kBucket = new();
            Bucket<Contact> actual;

            actual = kBucket._DetermineNode(Root, new byte[] {0x00}, 0);
            Assert.AreSame(Left, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 0);
            Assert.AreSame(Left, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 1);
            Assert.AreSame(Right, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 2);
            Assert.AreSame(Left, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 9);
            Assert.AreSame(Left, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x41}, 7);
            Assert.AreSame(Right, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x41, 0x00}, 7);
            Assert.AreSame(Right, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x00, 0x41, 0x00}, 15);
            Assert.AreSame(Right, actual);
        }
    }
}
