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

using System;
using System.Linq;
using MultiFormats;

namespace Lib.P2P.Tests
{
    public sealed class PeerTest
    {
        private const string MarsId = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        private const string PlutoId = "QmSoLPppuBtQSGwKDZT2M73ULpjvfd3aZ6ha4oFGL1KrGM";

        private const string MarsPublicKey =
            "CAASogEwgZ8wDQYJKoZIhvcNAQEBBQADgY0AMIGJAoGBAKGUtbRQf+a9SBHFEruNAUatS/tsGUnHuCtifGrlbYPELD3UyyhWf/FYczBCavx3i8hIPEW2jQv4ehxQxi/cg9SHswZCQblSi0ucwTBFr8d40JEiyB9CcapiMdFQxdMgGvXEOQdLz1pz+UPUDojkdKZq8qkkeiBn7KlAoGEocnmpAgMBAAE=";

        private static string marsAddress =
            "/ip4/10.1.10.10/tcp/29087/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";

        [Test]
        public new void ToString()
        {
            Assert.Equals("", new Peer().ToString());
            Assert.Equals(MarsId, new Peer {Id = MarsId}.ToString());
        }

        [Test]
        public void DefaultValues()
        {
            var peer = new Peer();
            Assert.Equals(null, peer.Id);
            Assert.Equals(0, peer.Addresses.Count());
            Assert.Equals("unknown/0.0", peer.ProtocolVersion);
            Assert.Equals("unknown/0.0", peer.AgentVersion);
            Assert.Equals(null, peer.PublicKey);
            Assert.Equals(false, peer.IsValid()); // missing peer ID
            Assert.Equals(null, peer.ConnectedAddress);
            Assert.That(peer.Latency.HasValue, Is.False);
        }

        [Test]
        public void ConnectedPeer()
        {
            var peer = new Peer
            {
                ConnectedAddress = new MultiAddress(marsAddress),
                Latency = TimeSpan.FromHours(3.03 * 2)
            };
            Assert.Equals(marsAddress, peer.ConnectedAddress.ToString());
            Assert.Equals(3.03 * 2, peer.Latency.Value.TotalHours);
        }

        [Test]
        public void Validation_No_Id()
        {
            var peer = new Peer();
            Assert.Equals(false, peer.IsValid());
        }

        [Test]
        public void Validation_With_Id()
        {
            Peer peer = MarsId;
            Assert.Equals(true, peer.IsValid());
        }

        [Test]
        public void Validation_With_Id_Pubkey()
        {
            var peer = new Peer
            {
                Id = MarsId,
                PublicKey = MarsPublicKey
            };
            Assert.Equals(true, peer.IsValid());
        }

        [Test]
        public void Validation_With_Id_Invalid_Pubkey()
        {
            var peer = new Peer
            {
                Id = PlutoId,
                PublicKey = MarsPublicKey
            };
            Assert.Equals(false, peer.IsValid());
        }

        [Test]
        public void Value_Equality()
        {
            var a0 = new Peer {Id = MarsId};
            var a1 = new Peer {Id = MarsId};
            var b = new Peer {Id = PlutoId};
            Peer c = null;
            Peer d = null;

            Assert.That(c == d, Is.True);
            Assert.That(c == b, Is.False);
            Assert.That(b == c, Is.False);

            Assert.That(c != d, Is.False);
            Assert.That(c != b, Is.True);
            Assert.That(b != c, Is.True);

#pragma warning disable 1718
            Assert.That(a0 == a0, Is.True);
            Assert.That(a0 == a1, Is.True);
            Assert.That(a0 == b, Is.False);

#pragma warning disable 1718
            Assert.That(a0 != a0, Is.False);
            Assert.That(a0 != a1, Is.False);
            Assert.That(a0 != b, Is.True);

            Assert.That(a0.Equals(a0), Is.True);
            Assert.That(a0.Equals(a1), Is.True);
            Assert.That(a0.Equals(b), Is.False);

            Assert.Equals(a0, a0);
            Assert.Equals(a0, a1);
            Assert.That(a0, Is.Not.EqualTo(b));

            Assert.Equals(a0, a0);
            Assert.Equals(a0, a1);
            Assert.That(a0, Is.Not.EqualTo(b));

            Assert.Equals(a0.GetHashCode(), a0.GetHashCode());
            Assert.Equals(a0.GetHashCode(), a1.GetHashCode());
            Assert.That(a0.GetHashCode(), Is.Not.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Implicit_Conversion_From_String()
        {
            Peer a = MarsId;
            Assert.That(a, Is.TypeOf(typeof(Peer)));
        }
    }
}
