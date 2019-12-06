﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.PubSub;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    [TestClass]
    public class PubSubApiTest
    {
        [Fact]
        public void Api_Exists()
        {
            var ipfs = TestFixture.Ipfs;
            Assert.NotNull(ipfs.PubSub);
        }

        [Fact]
        public void Peers_Unknown_Topic()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-unknown" + Guid.NewGuid().ToString();
            var peers = ipfs.PubSub.PeersAsync(topic).Result.ToArray();
            Assert.Equal(0, peers.Length);
        }

        [Fact]
        public async Task Subscribed_Topics()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg => { }, cs.Token);
                var topics = ipfs.PubSub.SubscribedTopicsAsync().Result.ToArray();
                Assert.True(topics.Length > 0);
                Assert.Contains(topics, topic);
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        volatile int messageCount = 0;

        [Fact]
        public async Task Subscribe()
        {
            messageCount = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg => { Interlocked.Increment(ref messageCount); }, cs.Token);
                await ipfs.PubSub.PublishAsync(topic, "hello world!");

                await Task.Delay(100);
                Assert.Equal(1, messageCount);
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        [Fact]
        public async Task Subscribe_Mutiple_Messages()
        {
            messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg => { Interlocked.Increment(ref messageCount); }, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSub.PublishAsync(topic, msg);
                }

                await Task.Delay(100);
                Assert.Equal(messages.Length, messageCount);
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        [Fact]
        public async Task Multiple_Subscribe_Mutiple_Messages()
        {
            messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();

            Action<IPublishedMessage> processMessage = (msg) => { Interlocked.Increment(ref messageCount); };
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, processMessage, cs.Token);
                await ipfs.PubSub.SubscribeAsync(topic, processMessage, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSub.PublishAsync(topic, msg);
                }

                await Task.Delay(100);
                Assert.Equal(messages.Length * 2, messageCount);
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        volatile int messageCount1 = 0;

        [Fact]
        public async Task Unsubscribe()
        {
            messageCount1 = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg => { Interlocked.Increment(ref messageCount1); }, cs.Token);
                await ipfs.PubSub.PublishAsync(topic, "hello world!");
                await Task.Delay(100);
                Assert.Equal(1, messageCount1);

                cs.Cancel();
                await ipfs.PubSub.PublishAsync(topic, "hello world!!!");
                await Task.Delay(100);
                Assert.Equal(1, messageCount1);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [Fact]
        public async Task Subscribe_BinaryMessage()
        {
            var messages = new List<IPublishedMessage>();
            var expected = new byte[] {0, 1, 2, 4, (byte) 'a', (byte) 'b', 0xfe, 0xff};
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg => { messages.Add(msg); }, cs.Token);
                await ipfs.PubSub.PublishAsync(topic, expected);

                await Task.Delay(100);
                Assert.Equal(1, messages.Count);
                Assert.Equal(expected, messages[0].DataBytes);
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        [Fact]
        public async Task Subscribe_StreamMessage()
        {
            var messages = new List<IPublishedMessage>();
            var expected = new byte[] {0, 1, 2, 4, (byte) 'a', (byte) 'b', 0xfe, 0xff};
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg => { messages.Add(msg); }, cs.Token);
                var ms = new MemoryStream(expected, false);
                await ipfs.PubSub.PublishAsync(topic, ms);

                await Task.Delay(100);
                Assert.Equal(1, messages.Count);
                Assert.Equal(expected, messages[0].DataBytes);
            }
            finally
            {
                cs.Cancel();
                await ipfs.StopAsync();
            }
        }
    }
}
