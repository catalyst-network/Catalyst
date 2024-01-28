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

using System.Linq;
using Lib.P2P.PubSub;

namespace Lib.P2P.Tests.PubSub
{
    public class TopicManagerTest
    {
        private Peer a = new Peer {Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH"};
        private Peer b = new Peer {Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"};

        [Test]
        public void Adding()
        {
            var topics = new TopicManager();
            Assert.Equals(0, topics.GetPeers("alpha").Count());

            topics.AddInterest("alpha", a);
            Assert.Equals(a, topics.GetPeers("alpha").First());

            topics.AddInterest("alpha", b);
            var peers = topics.GetPeers("alpha").ToArray();
            Assert.That(peers, Contains.Item(a));
            Assert.That(peers, Contains.Item(b));
        }

        [Test]
        public void Adding_Duplicate()
        {
            var topics = new TopicManager();
            Assert.Equals(0, topics.GetPeers("alpha").Count());

            topics.AddInterest("alpha", a);
            Assert.Equals(1, topics.GetPeers("alpha").Count());

            topics.AddInterest("alpha", a);
            Assert.Equals(1, topics.GetPeers("alpha").Count());

            topics.AddInterest("alpha", b);
            Assert.Equals(2, topics.GetPeers("alpha").Count());
        }

        [Test]
        public void Removing()
        {
            var topics = new TopicManager();
            Assert.Equals(0, topics.GetPeers("alpha").Count());

            topics.AddInterest("alpha", a);
            topics.AddInterest("alpha", b);
            Assert.Equals(2, topics.GetPeers("alpha").Count());

            topics.RemoveInterest("alpha", a);
            Assert.Equals(b, topics.GetPeers("alpha").First());
            Assert.Equals(1, topics.GetPeers("alpha").Count());

            topics.RemoveInterest("alpha", a);
            Assert.Equals(b, topics.GetPeers("alpha").First());
            Assert.Equals(1, topics.GetPeers("alpha").Count());

            topics.RemoveInterest("alpha", b);
            Assert.Equals(0, topics.GetPeers("alpha").Count());

            topics.RemoveInterest("beta", b);
            Assert.Equals(0, topics.GetPeers("beta").Count());
        }

        [Test]
        public void Clearing_Peers()
        {
            var topics = new TopicManager();
            Assert.Equals(0, topics.GetPeers("alpha").Count());
            Assert.Equals(0, topics.GetPeers("beta").Count());

            topics.AddInterest("alpha", a);
            topics.AddInterest("beta", a);
            topics.AddInterest("beta", b);
            Assert.Equals(1, topics.GetPeers("alpha").Count());
            Assert.Equals(2, topics.GetPeers("beta").Count());

            topics.Clear(a);
            Assert.Equals(0, topics.GetPeers("alpha").Count());
            Assert.Equals(1, topics.GetPeers("beta").Count());
        }

        [Test]
        public void Clearing()
        {
            var topics = new TopicManager();
            Assert.Equals(0, topics.GetPeers("alpha").Count());
            Assert.Equals(0, topics.GetPeers("beta").Count());

            topics.AddInterest("alpha", a);
            topics.AddInterest("beta", b);
            Assert.Equals(1, topics.GetPeers("alpha").Count());
            Assert.Equals(1, topics.GetPeers("beta").Count());

            topics.Clear();
            Assert.Equals(0, topics.GetPeers("alpha").Count());
            Assert.Equals(0, topics.GetPeers("beta").Count());
        }

        [Test]
        public void PeerTopics()
        {
            var tm = new TopicManager();
            tm.AddInterest("alpha", a);
            Assert.That(new[] {"alpha"}, Is.EquivalentTo(tm.GetTopics(a).ToArray()));
            Assert.That(new string[0], Is.EquivalentTo(tm.GetTopics(b).ToArray()));

            tm.AddInterest("beta", a);
            Assert.That(new[] {"alpha", "beta"}, Is.EquivalentTo(tm.GetTopics(a).ToArray()));
            Assert.That(new string[0], Is.EquivalentTo(tm.GetTopics(b).ToArray()));

            tm.AddInterest("beta", b);
            Assert.That(new[] {"alpha", "beta"}, Is.EquivalentTo(tm.GetTopics(a).ToArray()));
            Assert.That(new[] {"beta"}, Is.EquivalentTo(tm.GetTopics(b).ToArray()));
        }
    }
}
