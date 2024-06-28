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
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.PubSub;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests.PubSub
{
    [TestClass]
    public class FloodRouterTest
    {
        private Peer self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        private Peer other = new Peer
        {
            AgentVersion = "other",
            Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb",
            PublicKey =
                "CAASpgIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCfBYU9c0n28u02N/XCJY8yIsRqRVO5Zw+6kDHCremt2flHT4AaWnwGLAG9YyQJbRTvWN9nW2LK7Pv3uoIlvUSTnZEP0SXB5oZeqtxUdi6tuvcyqTIfsUSanLQucYITq8Qw3IMBzk+KpWNm98g9A/Xy30MkUS8mrBIO9pHmIZa55fvclDkTvLxjnGWA2avaBfJvHgMSTu0D2CQcmJrvwyKMhLCSIbQewZd2V7vc6gtxbRovKlrIwDTmDBXbfjbLljOuzg2yBLyYxXlozO9blpttbnOpU4kTspUVJXglmjsv7YSIJS3UKt3544l/srHbqlwC5CgOgjlwNfYPadO8kmBfAgMBAAE="
        };

        private Peer other1 = new Peer
        {
            AgentVersion = "other1",
            Id = "QmYSj5nkpHaJG6hDof33fv3YHnQfpFTNAd8jZ5GssgPygn",
            PublicKey =
                "CAASpgIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQC8s23axzV5S/fUoJ1+MT9fH1SzlDwqwdKoIirYAmvHnv6dyoaC7gMeHDJIc2gNnrvpdAoXyPxBS2Oysv/iHzseVi2kYvyU9pD5ZtiorzpV5oOXMfIfgGygXbiIk/DVQWD6Sq8flHY8ht+z69h9JL+Dj/aMfEzY5RoznJkikumoCn7QI6zvPZd9OPd7OyqcCZ31RThtIxrFd0YkHN+VV9pCq4iBfhMt8Ocy0RS/yrqaGE4PX2VsjExBmShEFnTFlhy0Mh4QhBLLquQH0aQEk2s5mZtwh7bKeW84zC0zIGWzcHrwVsHb+Z2/IXDTWNIlNGc/cCV7vAM1EgK1oQVf04NLAgMBAAE=",
        };

        [TestMethod]
        public void Defaults()
        {
            var router = new FloodRouter(new SwarmService(self));
            Assert.AreEqual("/floodsub/1.0.0", router.ToString());
        }

        [TestMethod]
        public void RemoteSubscriptions()
        {
            var router = new FloodRouter(new SwarmService(self));

            var sub = new Subscription { Topic = "topic", Subscribe = true };
            router.ProcessSubscription(sub, other);
            Assert.AreEqual(1, router.RemoteTopics.GetPeers("topic").Count());

            var can = new Subscription { Topic = "topic", Subscribe = false };
            router.ProcessSubscription(can, other);
            Assert.AreEqual(0, router.RemoteTopics.GetPeers("topic").Count());
        }

        [TestMethod]
        public async Task Sends_Hello_OnConnect()
        {
            var topic = Guid.NewGuid().ToString();

            var swarm1 = new SwarmService(self);
            var router1 = new FloodRouter(swarm1);
            var ns1 = new PubSubService(self, new[] { router1 });
            await swarm1.StartAsync();
            await ns1.StartAsync();

            var swarm2 = new SwarmService(other);
            var router2 = new FloodRouter(swarm2);
            var ns2 = new PubSubService(other, new[] { router2 });
            await swarm2.StartAsync();
            await ns2.StartAsync();

            try
            {
                await swarm1.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
                await swarm2.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

                var cs = new CancellationTokenSource();
                await ns1.SubscribeAsync(topic, msg => { }, cs.Token);
                await swarm1.ConnectAsync(other, cs.Token);

                var peers = new Peer[0];
                var endTime = DateTime.Now.AddSeconds(3);
                while (peers.Length == 0)
                {
                    if (DateTime.Now > endTime)
                    {
                        Assert.Fail("timeout");
                    }

                    await Task.Delay(100, cs.Token);
                    peers = (await ns2.PeersAsync(topic, cs.Token)).ToArray();
                }

                CollectionAssert.Contains(peers, self);
            }
            finally
            {
                await swarm1.StopAsync();
                await ns1.StopAsync();

                await swarm2.StopAsync();
                await ns2.StopAsync();
            }
        }

        [TestMethod]
        public async Task Sends_NewSubscription()
        {
            var topic = Guid.NewGuid().ToString();

            var swarm1 = new SwarmService(self);
            var router1 = new FloodRouter(swarm1);
            var ns1 = new PubSubService(self, new[] { router1 });
            await swarm1.StartAsync();
            await ns1.StartAsync();

            var swarm2 = new SwarmService(other);
            var router2 = new FloodRouter(swarm2);
            var ns2 = new PubSubService(other, new[] { router2 });
            await swarm2.StartAsync();
            await ns2.StartAsync();

            try
            {
                await swarm1.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
                await swarm2.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

                var cs = new CancellationTokenSource();
                await swarm1.ConnectAsync(other, cs.Token);
                await ns1.SubscribeAsync(topic, msg => { }, cs.Token);

                var peers = new Peer[0];
                var endTime = DateTime.Now.AddSeconds(3);
                while (peers.Length == 0)
                {
                    if (DateTime.Now > endTime)
                    {
                        Assert.Fail("timeout");
                    }

                    await Task.Delay(100, cs.Token);
                    peers = (await ns2.PeersAsync(topic, cs.Token)).ToArray();
                }

                CollectionAssert.Contains(peers, self);
            }
            finally
            {
                await swarm1.StopAsync();
                await ns1.StopAsync();

                await swarm2.StopAsync();
                await ns2.StopAsync();
            }
        }

        [TestMethod]
        public async Task Sends_CancelledSubscription()
        {
            var topic = Guid.NewGuid().ToString();

            var swarm1 = new SwarmService(self);
            var router1 = new FloodRouter(swarm1);
            var ns1 = new PubSubService(self, new[] { router1 });
            await swarm1.StartAsync();
            await ns1.StartAsync();

            var swarm2 = new SwarmService(other);
            var router2 = new FloodRouter(swarm2);
            var ns2 = new PubSubService(other, new[] { router2 });
            await swarm2.StartAsync();
            await ns2.StartAsync();

            try
            {
                await swarm1.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
                await swarm2.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

                var cs = new CancellationTokenSource();
                await swarm1.ConnectAsync(other, cs.Token);
                await ns1.SubscribeAsync(topic, msg => { }, cs.Token);

                var peers = new Peer[0];
                var endTime = DateTime.Now.AddSeconds(3);
                while (peers.Length == 0)
                {
                    if (DateTime.Now > endTime)
                    {
                        Assert.Fail("timeout");
                    }

                    await Task.Delay(100, cs.Token);
                    peers = (await ns2.PeersAsync(topic, cs.Token)).ToArray();
                }

                CollectionAssert.Contains(peers, self);

                cs.Cancel();
                peers = new Peer[0];
                endTime = DateTime.Now.AddSeconds(3);
                while (peers.Length != 0)
                {
                    if (DateTime.Now > endTime)
                    {
                        Assert.Fail("timeout");
                    }

                    await Task.Delay(100, cs.Token);
                    peers = (await ns2.PeersAsync(topic, cs.Token)).ToArray();
                }
            }
            finally
            {
                await swarm1.StopAsync();
                await ns1.StopAsync();

                await swarm2.StopAsync();
                await ns2.StopAsync();
            }
        }

        [TestMethod]
        public async Task Relays_PublishedMessage()
        {
            var topic = Guid.NewGuid().ToString();

            var swarm1 = new SwarmService(self);
            var router1 = new FloodRouter(swarm1);
            var ns1 = new PubSubService(self, new[] { router1 });
            await swarm1.StartAsync();
            await ns1.StartAsync();

            var swarm2 = new SwarmService(other);
            var router2 = new FloodRouter(swarm2);
            var ns2 = new PubSubService(other, new[] { router2 });
            await swarm2.StartAsync();
            await ns2.StartAsync();

            var swarm3 = new SwarmService(other1);
            var router3 = new FloodRouter(swarm3);
            var ns3 = new PubSubService(other1, new[] { router3 });
            await swarm3.StartAsync();
            await ns3.StartAsync();

            try
            {
                IPublishedMessage lastMessage2 = null;
                IPublishedMessage lastMessage3 = null;
                await swarm1.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
                await swarm2.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
                await swarm3.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

                var cs = new CancellationTokenSource();
                await ns2.SubscribeAsync(topic, msg => lastMessage2 = msg, cs.Token);
                await ns3.SubscribeAsync(topic, msg => lastMessage3 = msg, cs.Token);
                await swarm1.ConnectAsync(other, cs.Token);
                await swarm3.ConnectAsync(other, cs.Token);

                var peers = new Peer[0];
                var endTime = DateTime.Now.AddSeconds(3);
                while (peers.Length == 0)
                {
                    if (DateTime.Now > endTime)
                    {
                        Assert.Fail("timeout");
                    }

                    await Task.Delay(100, cs.Token);
                    peers = (await ns2.PeersAsync(topic, cs.Token)).ToArray();
                }

                CollectionAssert.Contains(peers, other1);

                await ns1.PublishAsync(topic, new byte[] { 1 }, cs.Token);
                endTime = DateTime.Now.AddSeconds(3);
                while (lastMessage2 == null || lastMessage3 == null)
                {
                    if (DateTime.Now > endTime)
                    {
                        Assert.Fail("timeout");
                    }

                    await Task.Delay(100, cs.Token);
                }

                Assert.IsNotNull(lastMessage2);
                Assert.AreEqual(self, lastMessage2.Sender);
                CollectionAssert.AreEqual(new byte[] { 1 }, lastMessage2.DataBytes);
                CollectionAssert.Contains(lastMessage2.Topics.ToArray(), topic);

                Assert.IsNotNull(lastMessage3);
                Assert.AreEqual(self, lastMessage3.Sender);
                CollectionAssert.AreEqual(new byte[] { 1 }, lastMessage3.DataBytes);
                CollectionAssert.Contains(lastMessage3.Topics.ToArray(), topic);
            }
            finally
            {
                await swarm1.StopAsync();
                await ns1.StopAsync();

                await swarm2.StopAsync();
                await ns2.StopAsync();

                await swarm3.StopAsync();
                await ns3.StopAsync();
            }
        }
    }
}
