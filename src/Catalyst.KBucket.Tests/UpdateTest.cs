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
    /// <summary>
    ///   From https://github.com/tristanls/k-bucket/blob/master/test/update.js
    /// </summary>
    [TestClass]
    public class UpdateTest
    {
        [TestMethod]
        public void ContactDrop()
        {
            var kBucket = new KBucket<Contact>
            {
                Arbiter = (a, b) => a.Clock > b.Clock ? a : b
            };
            Contact a3 = new("a") {Clock = 3};
            Contact a2 = new("a") {Clock = 2};
            Contact a4 = new("a") {Clock = 4};

            kBucket.Add(a3);
            kBucket.Add(a2);
            Assert.AreEqual(1, kBucket.Count);
            Assert.IsTrue(kBucket.TryGet(a3.Id, out var current));
            Assert.AreSame(a3, current);

            kBucket.Add(a4);
            Assert.AreEqual(1, kBucket.Count);
            Assert.IsTrue(kBucket.TryGet(a4.Id, out current));
            Assert.AreSame(a4, current);
        }
    }
}
