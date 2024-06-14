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

using MultiFormats;
using System.Linq;

namespace Lib.P2P.Tests
{
    public sealed class MultiAddressWhiteListTest
    {
        private readonly MultiAddress _a = "/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        private readonly MultiAddress _a1 = "/ip4/127.0.0.1/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        private readonly MultiAddress _b = "/p2p/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        private readonly MultiAddress _c = "/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64";
        private readonly MultiAddress _d = "/p2p/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64";

        [Test]
        public void Allowed()
        {
            var policy = new MultiAddressWhiteList {_a, _b};
            Assert.That(policy.IsAllowed(_a), Is.True);
            Assert.That(policy.IsAllowed(_a1), Is.True);
            Assert.That(policy.IsAllowed(_b), Is.True);
            Assert.That(policy.IsAllowed(_c), Is.False);
            Assert.That(policy.IsAllowed(_d), Is.False);
        }

        [Test]
        public void Allowed_Alias()
        {
            var policy = new MultiAddressWhiteList {_a};
            Assert.That(policy.IsAllowed(_a), Is.True);
            Assert.That(policy.IsAllowed(_a1), Is.True);
            Assert.That(policy.IsAllowed(_b), Is.True);
            Assert.That(policy.IsAllowed(_c), Is.False);
            Assert.That(policy.IsAllowed(_d), Is.False);
        }

        [Test]
        public void Empty()
        {
            var policy = new MultiAddressWhiteList();
            Assert.That(policy.IsAllowed(_a), Is.True);
        }

        [Test]
        public void Collection()
        {
            MultiAddress addressA = "/ip4/127.0.0.1";
            MultiAddress addressB = "/ip4/127.0.0.2";

            var policy = new MultiAddressWhiteList();
            Assert.That(policy.IsReadOnly, Is.False);
            Assert.That(policy, Has.Count.EqualTo(0));
            Assert.That(policy.Contains(addressA), Is.False);
            Assert.That(policy.Contains(addressB), Is.False);

            policy.Add(addressA);
            Assert.That(policy, Has.Count.EqualTo(1));
            Assert.That(policy.Contains(addressA), Is.True);
            Assert.That(policy.Contains(addressB), Is.False);

            policy.Add(addressA);
            Assert.That(policy, Has.Count.EqualTo(1));
            Assert.That(policy.Contains(addressA), Is.True);
            Assert.That(policy.Contains(addressB), Is.False);

            policy.Add(addressB);
            Assert.That(policy, Has.Count.EqualTo(2));
            Assert.That(policy.Contains(addressA), Is.True);
            Assert.That(policy.Contains(addressB), Is.True);

            policy.Remove(addressB);
            Assert.That(policy, Has.Count.EqualTo(1));
            Assert.That(policy.Contains(addressA), Is.True);
            Assert.That(policy.Contains(addressB), Is.False);

            var array = new MultiAddress[1];
            policy.CopyTo(array, 0);
            Assert.That(addressA, Is.EqualTo(array[0]));

            foreach (var filter in policy)
            {
                Assert.That(addressA, Is.EqualTo(filter));
            }

            policy.Clear();
            Assert.That(policy.Count, Is.EqualTo(0));
            Assert.That(policy.Contains(addressA), Is.False);
            Assert.That(policy.Contains(addressB), Is.False);
        }
    }
}
