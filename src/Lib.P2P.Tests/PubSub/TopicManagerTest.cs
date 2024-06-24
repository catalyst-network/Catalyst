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
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(0));

            topics.AddInterest("alpha", a);
            Assert.That(a, Is.EqualTo(topics.GetPeers("alpha").First()));

            topics.AddInterest("alpha", b);
            var peers = topics.GetPeers("alpha").ToArray();
            Assert.That(peers, Contains.Item(a));
            Assert.That(peers, Contains.Item(b));
        }

        [Test]
        public void Adding_Duplicate()
        {
            var topics = new TopicManager();
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(0));

            topics.AddInterest("alpha", a);
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(1));

            topics.AddInterest("alpha", a);
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(1));

            topics.AddInterest("alpha", b);
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(2));
        }

        [Test]
        public void Removing()
        {
            var topics = new TopicManager();
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(0));

            topics.AddInterest("alpha", a);
            topics.AddInterest("alpha", b);
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(2));

            topics.RemoveInterest("alpha", a);
            Assert.That(b, Is.EqualTo(topics.GetPeers("alpha").First()));
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(1));

            topics.RemoveInterest("alpha", a);
            Assert.That(b, Is.EqualTo(topics.GetPeers("alpha").First()));
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(1));

            topics.RemoveInterest("alpha", b);
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(0));

            topics.RemoveInterest("beta", b);
            Assert.That(topics.GetPeers("beta").Count(), Is.EqualTo(0));
        }

        [Test]
        public void Clearing_Peers()
        {
            var topics = new TopicManager();
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(0));
            Assert.That(topics.GetPeers("beta").Count(), Is.EqualTo(0));

            topics.AddInterest("alpha", a);
            topics.AddInterest("beta", a);
            topics.AddInterest("beta", b);
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(1));
            Assert.That(topics.GetPeers("beta").Count(), Is.EqualTo(2));

            topics.Clear(a);
            Assert.That(topics.GetPeers("alph)").Count(), Is.EqualTo(0));
            Assert.That(topics.GetPeers("beta").Count(), Is.EqualTo(1));
        }

        [Test]
        public void Clearing()
        {
            var topics = new TopicManager();
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(0));
            Assert.That(topics.GetPeers("beta").Count(), Is.EqualTo(0));

            topics.AddInterest("alpha", a);
            topics.AddInterest("beta", b);
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(1));
            Assert.That(topics.GetPeers("beta").Count(), Is.EqualTo(1));

            topics.Clear();
            Assert.That(topics.GetPeers("alpha").Count(), Is.EqualTo(0));
            Assert.That(topics.GetPeers("beta").Count(), Is.EqualTo(0));
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
