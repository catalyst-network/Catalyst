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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.PubSub;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests.PubSub
{
    [TestClass]
    public class NotificationServiceTest
    {
        private Peer self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        private Peer other1 = new Peer { Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ" };
        private Peer other2 = new Peer { Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvUJ" };

        [TestMethod]
        public async Task MessageID_Increments()
        {
            var ns = new PubSubService(self, new[] { new LoopbackRouter() });
            await ns.StartAsync();
            try
            {
                var a = ns.CreateMessage("topic", new byte[0]);
                var b = ns.CreateMessage("topic", new byte[0]);
                Assert.IsTrue(string.Compare(b.MessageId, a.MessageId, StringComparison.Ordinal) > 0);
            }
            finally
            {
                await ns.StopAsync();
            }
        }

        [TestMethod]
        public async Task Publish()
        {
            var ns = new PubSubService(self, new[] { new LoopbackRouter() });
            await ns.StartAsync();
            try
            {
                await ns.PublishAsync("topic", "foo");
                await ns.PublishAsync("topic", new byte[] { 1, 2, 3 });
                await ns.PublishAsync("topic", new MemoryStream(new byte[] { 1, 2, 3 }));
                Assert.AreEqual(3ul, ns.MesssagesPublished);
                Assert.AreEqual(3ul, ns.MesssagesReceived);
            }
            finally
            {
                await ns.StopAsync();
            }
        }

        [TestMethod]
        public async Task Topics()
        {
            var ns = new PubSubService(self, new[] { new LoopbackRouter() });
            await ns.StartAsync();
            try
            {
                var topicA = Guid.NewGuid().ToString();
                var topicB = Guid.NewGuid().ToString();
                var csA = new CancellationTokenSource();
                var csB = new CancellationTokenSource();

                await ns.SubscribeAsync(topicA, msg => { }, csA.Token);
                await ns.SubscribeAsync(topicA, msg => { }, csA.Token);
                await ns.SubscribeAsync(topicB, msg => { }, csB.Token);

                var topics = (await ns.SubscribedTopicsAsync(csA.Token)).ToArray();
                Assert.AreEqual(2, topics.Length);
                CollectionAssert.Contains(topics, topicA);
                CollectionAssert.Contains(topics, topicB);

                csA.Cancel();
                topics = (await ns.SubscribedTopicsAsync(csA.Token)).ToArray();
                Assert.AreEqual(1, topics.Length);
                CollectionAssert.Contains(topics, topicB);

                csB.Cancel();
                topics = (await ns.SubscribedTopicsAsync(csA.Token)).ToArray();
                Assert.AreEqual(0, topics.Length);
            }
            finally
            {
                await ns.StopAsync();
            }
        }

        [TestMethod]
        public async Task Subscribe()
        {
            var ns = new PubSubService(self, new[] { new LoopbackRouter() });
            await ns.StartAsync();
            try
            {
                var topic = Guid.NewGuid().ToString();
                var cs = new CancellationTokenSource();
                var messageCount = 0;
                await ns.SubscribeAsync(topic, msg => { ++messageCount; }, cs.Token);
                await ns.SubscribeAsync(topic, msg => { ++messageCount; }, cs.Token);

                await ns.PublishAsync(topic, "", cs.Token);
                Assert.AreEqual(2, messageCount);
            }
            finally
            {
                await ns.StopAsync();
            }
        }

        [TestMethod]
        public async Task Subscribe_HandlerExceptionIsIgnored()
        {
            var ns = new PubSubService(self, new[] { new LoopbackRouter() });
            await ns.StartAsync();
            try
            {
                var topic = Guid.NewGuid().ToString();
                var cs = new CancellationTokenSource();
                var messageCount = 0;
                await ns.SubscribeAsync(topic, msg =>
                {
                    ++messageCount;
                    throw new Exception();
                }, cs.Token);

                await ns.PublishAsync(topic, "", cs.Token);
                Assert.AreEqual(1, messageCount);
            }
            finally
            {
                await ns.StopAsync();
            }
        }

        [TestMethod]
        public async Task DuplicateMessagesAreIgnored()
        {
            var ns = new PubSubService(self, new[] { new LoopbackRouter() });
            ns.Routers.Add(new LoopbackRouter());
            await ns.StartAsync();
            try
            {
                var topic = Guid.NewGuid().ToString();
                var cs = new CancellationTokenSource();
                var messageCount = 0;
                await ns.SubscribeAsync(topic, msg => { ++messageCount; }, cs.Token);

                await ns.PublishAsync(topic, "", cs.Token);
                Assert.AreEqual(1, messageCount);
                Assert.AreEqual(2ul, ns.MesssagesReceived);
                Assert.AreEqual(1ul, ns.DuplicateMesssagesReceived);
            }
            finally
            {
                await ns.StopAsync();
            }
        }

        [TestMethod]
        public async Task SubscribedPeers_ForTopic()
        {
            var topic1 = Guid.NewGuid().ToString();
            var topic2 = Guid.NewGuid().ToString();
            var router = new FloodRouter(new SwarmService(self));
            var ns = new PubSubService(self, new[] { router });
            router.RemoteTopics.AddInterest(topic1, other1);
            router.RemoteTopics.AddInterest(topic2, other2);
            ns.Routers.Add(router);
            await ns.StartAsync();
            try
            {
                var peers = (await ns.PeersAsync(topic1)).ToArray();
                Assert.AreEqual(1, peers.Length);
                Assert.AreEqual(other1, peers[0]);

                peers = (await ns.PeersAsync(topic2)).ToArray();
                Assert.AreEqual(1, peers.Length);
                Assert.AreEqual(other2, peers[0]);
            }
            finally
            {
                await ns.StopAsync();
            }
        }

        [TestMethod]
        public async Task SubscribedPeers_AllTopics()
        {
            var topic1 = Guid.NewGuid().ToString();
            var topic2 = Guid.NewGuid().ToString();
            var router = new FloodRouter(new SwarmService(self));
            var ns = new PubSubService(self, new[] { router });
            router.RemoteTopics.AddInterest(topic1, other1);
            router.RemoteTopics.AddInterest(topic2, other2);
            ns.Routers.Add(router);
            await ns.StartAsync();
            try
            {
                var peers = (await ns.PeersAsync()).ToArray();
                Assert.AreEqual(2, peers.Length);
            }
            finally
            {
                await ns.StopAsync();
            }
        }
    }
}
