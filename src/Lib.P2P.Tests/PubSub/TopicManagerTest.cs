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

using System.Linq;
using Lib.P2P.PubSub;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests.PubSub
{
    [TestClass]
    public class TopicManagerTest
    {
        private Peer a = new Peer {Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH"};
        private Peer b = new Peer {Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"};

        [TestMethod]
        public void Adding()
        {
            var topics = new TopicManager();
            Assert.AreEqual(0, topics.GetPeers("alpha").Count());

            topics.AddInterest("alpha", a);
            Assert.AreEqual(a, topics.GetPeers("alpha").First());

            topics.AddInterest("alpha", b);
            var peers = topics.GetPeers("alpha").ToArray();
            CollectionAssert.Contains(peers, a);
            CollectionAssert.Contains(peers, b);
        }

        [TestMethod]
        public void Adding_Duplicate()
        {
            var topics = new TopicManager();
            Assert.AreEqual(0, topics.GetPeers("alpha").Count());

            topics.AddInterest("alpha", a);
            Assert.AreEqual(1, topics.GetPeers("alpha").Count());

            topics.AddInterest("alpha", a);
            Assert.AreEqual(1, topics.GetPeers("alpha").Count());

            topics.AddInterest("alpha", b);
            Assert.AreEqual(2, topics.GetPeers("alpha").Count());
        }

        [TestMethod]
        public void Removing()
        {
            var topics = new TopicManager();
            Assert.AreEqual(0, topics.GetPeers("alpha").Count());

            topics.AddInterest("alpha", a);
            topics.AddInterest("alpha", b);
            Assert.AreEqual(2, topics.GetPeers("alpha").Count());

            topics.RemoveInterest("alpha", a);
            Assert.AreEqual(b, topics.GetPeers("alpha").First());
            Assert.AreEqual(1, topics.GetPeers("alpha").Count());

            topics.RemoveInterest("alpha", a);
            Assert.AreEqual(b, topics.GetPeers("alpha").First());
            Assert.AreEqual(1, topics.GetPeers("alpha").Count());

            topics.RemoveInterest("alpha", b);
            Assert.AreEqual(0, topics.GetPeers("alpha").Count());

            topics.RemoveInterest("beta", b);
            Assert.AreEqual(0, topics.GetPeers("beta").Count());
        }

        [TestMethod]
        public void Clearing_Peers()
        {
            var topics = new TopicManager();
            Assert.AreEqual(0, topics.GetPeers("alpha").Count());
            Assert.AreEqual(0, topics.GetPeers("beta").Count());

            topics.AddInterest("alpha", a);
            topics.AddInterest("beta", a);
            topics.AddInterest("beta", b);
            Assert.AreEqual(1, topics.GetPeers("alpha").Count());
            Assert.AreEqual(2, topics.GetPeers("beta").Count());

            topics.Clear(a);
            Assert.AreEqual(0, topics.GetPeers("alpha").Count());
            Assert.AreEqual(1, topics.GetPeers("beta").Count());
        }

        [TestMethod]
        public void Clearing()
        {
            var topics = new TopicManager();
            Assert.AreEqual(0, topics.GetPeers("alpha").Count());
            Assert.AreEqual(0, topics.GetPeers("beta").Count());

            topics.AddInterest("alpha", a);
            topics.AddInterest("beta", b);
            Assert.AreEqual(1, topics.GetPeers("alpha").Count());
            Assert.AreEqual(1, topics.GetPeers("beta").Count());

            topics.Clear();
            Assert.AreEqual(0, topics.GetPeers("alpha").Count());
            Assert.AreEqual(0, topics.GetPeers("beta").Count());
        }

        [TestMethod]
        public void PeerTopics()
        {
            var tm = new TopicManager();
            tm.AddInterest("alpha", a);
            CollectionAssert.AreEquivalent(new[] {"alpha"}, tm.GetTopics(a).ToArray());
            CollectionAssert.AreEquivalent(new string[0], tm.GetTopics(b).ToArray());

            tm.AddInterest("beta", a);
            CollectionAssert.AreEquivalent(new[] {"alpha", "beta"}, tm.GetTopics(a).ToArray());
            CollectionAssert.AreEquivalent(new string[0], tm.GetTopics(b).ToArray());

            tm.AddInterest("beta", b);
            CollectionAssert.AreEquivalent(new[] {"alpha", "beta"}, tm.GetTopics(a).ToArray());
            CollectionAssert.AreEquivalent(new[] {"beta"}, tm.GetTopics(b).ToArray());
        }
    }
}
