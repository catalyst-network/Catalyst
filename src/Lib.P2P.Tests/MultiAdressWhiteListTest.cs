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
using MultiFormats;

namespace Lib.P2P.Tests
{
    [TestClass]
    public sealed class MultiAddressWhiteListTest
    {
        private readonly MultiAddress _a = "/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        private readonly MultiAddress _a1 = "/ip4/127.0.0.1/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        private readonly MultiAddress _b = "/p2p/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        private readonly MultiAddress _c = "/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64";
        private readonly MultiAddress _d = "/p2p/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64";

        [TestMethod]
        public void Allowed()
        {
            var policy = new MultiAddressWhiteList {_a, _b};
            Assert.IsTrue(policy.IsAllowed(_a));
            Assert.IsTrue(policy.IsAllowed(_a1));
            Assert.IsTrue(policy.IsAllowed(_b));
            Assert.IsFalse(policy.IsAllowed(_c));
            Assert.IsFalse(policy.IsAllowed(_d));
        }

        [TestMethod]
        public void Allowed_Alias()
        {
            var policy = new MultiAddressWhiteList {_a};
            Assert.IsTrue(policy.IsAllowed(_a));
            Assert.IsTrue(policy.IsAllowed(_a1));
            Assert.IsTrue(policy.IsAllowed(_b));
            Assert.IsFalse(policy.IsAllowed(_c));
            Assert.IsFalse(policy.IsAllowed(_d));
        }

        [TestMethod]
        public void Empty()
        {
            var policy = new MultiAddressWhiteList();
            Assert.IsTrue(policy.IsAllowed(_a));
        }

        [TestMethod]
        public void Collection()
        {
            MultiAddress addressA = "/ip4/127.0.0.1";
            MultiAddress addressB = "/ip4/127.0.0.2";

            var policy = new MultiAddressWhiteList();
            Assert.IsFalse(policy.IsReadOnly);
            Assert.AreEqual(0, policy.Count);
            Assert.IsFalse(policy.Contains(addressA));
            Assert.IsFalse(policy.Contains(addressB));

            policy.Add(addressA);
            Assert.AreEqual(1, policy.Count);
            Assert.IsTrue(policy.Contains(addressA));
            Assert.IsFalse(policy.Contains(addressB));

            policy.Add(addressA);
            Assert.AreEqual(1, policy.Count);
            Assert.IsTrue(policy.Contains(addressA));
            Assert.IsFalse(policy.Contains(addressB));

            policy.Add(addressB);
            Assert.AreEqual(2, policy.Count);
            Assert.IsTrue(policy.Contains(addressA));
            Assert.IsTrue(policy.Contains(addressB));

            policy.Remove(addressB);
            Assert.AreEqual(1, policy.Count);
            Assert.IsTrue(policy.Contains(addressA));
            Assert.IsFalse(policy.Contains(addressB));

            var array = new MultiAddress[1];
            policy.CopyTo(array, 0);
            Assert.AreSame(addressA, array[0]);

            foreach (var filter in policy)
            {
                Assert.AreSame(addressA, filter);
            }

            policy.Clear();
            Assert.AreEqual(0, policy.Count);
            Assert.IsFalse(policy.Contains(addressA));
            Assert.IsFalse(policy.Contains(addressB));
        }
    }
}
