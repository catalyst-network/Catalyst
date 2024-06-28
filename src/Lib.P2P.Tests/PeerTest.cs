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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiFormats;

namespace Lib.P2P.Tests
{
    [TestClass]
    public sealed class PeerTest
    {
        private const string MarsId = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        private const string PlutoId = "QmSoLPppuBtQSGwKDZT2M73ULpjvfd3aZ6ha4oFGL1KrGM";

        private const string MarsPublicKey =
            "CAASogEwgZ8wDQYJKoZIhvcNAQEBBQADgY0AMIGJAoGBAKGUtbRQf+a9SBHFEruNAUatS/tsGUnHuCtifGrlbYPELD3UyyhWf/FYczBCavx3i8hIPEW2jQv4ehxQxi/cg9SHswZCQblSi0ucwTBFr8d40JEiyB9CcapiMdFQxdMgGvXEOQdLz1pz+UPUDojkdKZq8qkkeiBn7KlAoGEocnmpAgMBAAE=";

        private static string marsAddress =
            "/ip4/10.1.10.10/tcp/29087/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";

        [TestMethod]
        public new void ToString()
        {
            Assert.AreEqual("", new Peer().ToString());
            Assert.AreEqual(MarsId, new Peer {Id = MarsId}.ToString());
        }

        [TestMethod]
        public void DefaultValues()
        {
            var peer = new Peer();
            Assert.AreEqual(null, peer.Id);
            Assert.AreEqual(0, peer.Addresses.Count());
            Assert.AreEqual("unknown/0.0", peer.ProtocolVersion);
            Assert.AreEqual("unknown/0.0", peer.AgentVersion);
            Assert.AreEqual(null, peer.PublicKey);
            Assert.AreEqual(false, peer.IsValid()); // missing peer ID
            Assert.AreEqual(null, peer.ConnectedAddress);
            Assert.IsFalse(peer.Latency.HasValue);
        }

        [TestMethod]
        public void ConnectedPeer()
        {
            var peer = new Peer
            {
                ConnectedAddress = new MultiAddress(marsAddress),
                Latency = TimeSpan.FromHours(3.03 * 2)
            };
            Assert.AreEqual(marsAddress, peer.ConnectedAddress.ToString());
            Assert.AreEqual(3.03 * 2, peer.Latency.Value.TotalHours);
        }

        [TestMethod]
        public void Validation_No_Id()
        {
            var peer = new Peer();
            Assert.AreEqual(false, peer.IsValid());
        }

        [TestMethod]
        public void Validation_With_Id()
        {
            Peer peer = MarsId;
            Assert.AreEqual(true, peer.IsValid());
        }

        [TestMethod]
        public void Validation_With_Id_Pubkey()
        {
            var peer = new Peer
            {
                Id = MarsId,
                PublicKey = MarsPublicKey
            };
            Assert.AreEqual(true, peer.IsValid());
        }

        [TestMethod]
        public void Validation_With_Id_Invalid_Pubkey()
        {
            var peer = new Peer
            {
                Id = PlutoId,
                PublicKey = MarsPublicKey
            };
            Assert.AreEqual(false, peer.IsValid());
        }

        [TestMethod]
        public void Value_Equality()
        {
            var a0 = new Peer {Id = MarsId};
            var a1 = new Peer {Id = MarsId};
            var b = new Peer {Id = PlutoId};
            Peer c = null;
            Peer d = null;

            Assert.IsTrue(c == d);
            Assert.IsFalse(c == b);
            Assert.IsFalse(b == c);

            Assert.IsFalse(c != d);
            Assert.IsTrue(c != b);
            Assert.IsTrue(b != c);

#pragma warning disable 1718
            Assert.IsTrue(a0 == a0);
            Assert.IsTrue(a0 == a1);
            Assert.IsFalse(a0 == b);

#pragma warning disable 1718
            Assert.IsFalse(a0 != a0);
            Assert.IsFalse(a0 != a1);
            Assert.IsTrue(a0 != b);

            Assert.IsTrue(a0.Equals(a0));
            Assert.IsTrue(a0.Equals(a1));
            Assert.IsFalse(a0.Equals(b));

            Assert.AreEqual(a0, a0);
            Assert.AreEqual(a0, a1);
            Assert.AreNotEqual(a0, b);

            Assert.AreEqual(a0, a0);
            Assert.AreEqual(a0, a1);
            Assert.AreNotEqual(a0, b);

            Assert.AreEqual(a0.GetHashCode(), a0.GetHashCode());
            Assert.AreEqual(a0.GetHashCode(), a1.GetHashCode());
            Assert.AreNotEqual(a0.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void Implicit_Conversion_From_String()
        {
            Peer a = MarsId;
            Assert.IsInstanceOfType(a, typeof(Peer));
        }
    }
}
