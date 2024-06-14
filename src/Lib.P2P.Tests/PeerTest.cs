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
            Assert.That(new Peer().ToString(), Is.EqualTo(""));
            Assert.That(MarsId, Is.EqualTo(new Peer {Id = MarsId}.ToString()));
        }

        [Test]
        public void DefaultValues()
        {
            var peer = new Peer();
            Assert.That(peer.Id, Is.Null);
            Assert.That(0, Is.EqualTo(peer.Addresses.Count()));
            Assert.That("unknown/0.0", Is.EqualTo(peer.ProtocolVersion));
            Assert.That("unknown/0.0", Is.EqualTo(peer.AgentVersion));
            Assert.That(peer.PublicKey, Is.Null);
            Assert.That(false, Is.EqualTo(peer.IsValid())); // missing peer ID
            Assert.That(peer.ConnectedAddress, Is.Null);
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
            Assert.That(marsAddress, Is.EqualTo(peer.ConnectedAddress.ToString()));
            Assert.That(3.03 * 2, Is.EqualTo(peer.Latency.Value.TotalHours));
        }

        [Test]
        public void Validation_No_Id()
        {
            var peer = new Peer();
            Assert.That(false, Is.EqualTo(peer.IsValid()));
        }

        [Test]
        public void Validation_With_Id()
        {
            Peer peer = MarsId;
            Assert.That(true, Is.EqualTo(peer.IsValid()));
        }

        [Test]
        public void Validation_With_Id_Pubkey()
        {
            var peer = new Peer
            {
                Id = MarsId,
                PublicKey = MarsPublicKey
            };
            Assert.That(true, Is.EqualTo(peer.IsValid()));
        }

        [Test]
        public void Validation_With_Id_Invalid_Pubkey()
        {
            var peer = new Peer
            {
                Id = PlutoId,
                PublicKey = MarsPublicKey
            };
            Assert.That(false, Is.EqualTo(peer.IsValid()));
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

            Assert.That(a0, Is.EqualTo(a0));
            Assert.That(a0, Is.EqualTo(a1));
            Assert.That(a0, Is.Not.EqualTo(b));

            Assert.That(a0, Is.EqualTo(a0));
            Assert.That(a0, Is.EqualTo(a1));
            Assert.That(a0, Is.Not.EqualTo(b));

            Assert.That(a0.GetHashCode(), Is.EqualTo(a0.GetHashCode()));
            Assert.That(a0.GetHashCode(), Is.EqualTo(a1.GetHashCode()));
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
