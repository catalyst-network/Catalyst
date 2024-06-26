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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.TestUtils;
using FluentAssertions;
using Lib.P2P.PubSub;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class PubSubApiTest
    {
        private IDfsService ipfs;

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ipfs.Dispose();
        }

        public PubSubApiTest()
        {
            ipfs = TestDfs.GetTestDfs();
        }
        
        [Test]
        public void Api_Exists()
        {
            Assert.That(ipfs.PubSubApi, Is.Not.Null);
        }

        [Test]
        public void Peers_Unknown_Topic()
        {
            var topic = "net-ipfs-http-client-test-unknown" + Guid.NewGuid();
            var peers = ipfs.PubSubApi.PeersAsync(topic).Result.ToArray();
            Assert.That(peers.Length, Is.EqualTo(0));
        }

        [Test]
        public async Task Subscribed_Topics()
        {
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { }, cs.Token);
                var topics = ipfs.PubSubApi.SubscribedTopicsAsync(cs.Token).Result.ToArray();
                Assert.That(topics.Length > 0, Is.True);
                topics.Should().Contain(topic);
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        private volatile int _messageCount;

        [Test]
        public async Task Subscribe()
        {
            _messageCount = 0;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { Interlocked.Increment(ref _messageCount); }, cs.Token);
                await ipfs.PubSubApi.PublishAsync(topic, "hello world!", cs.Token);

                await Task.Delay(100, cs.Token);
                Assert.That(_messageCount, Is.EqualTo(1));
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        [Test]
        public async Task Subscribe_Mutiple_Messages()
        {
            _messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { Interlocked.Increment(ref _messageCount); }, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSubApi.PublishAsync(topic, msg, cs.Token);
                }

                await Task.Delay(100, cs.Token);
                Assert.That(messages.Length, Is.EqualTo(_messageCount));
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        [Test]
        public async Task Multiple_Subscribe_Mutiple_Messages()
        {
            _messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();

            void ProcessMessage(IPublishedMessage msg) { Interlocked.Increment(ref _messageCount); }
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, ProcessMessage, cs.Token);
                await ipfs.PubSubApi.SubscribeAsync(topic, ProcessMessage, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSubApi.PublishAsync(topic, msg, cs.Token);
                }

                await Task.Delay(100, cs.Token);
                Assert.That(messages.Length * 2, Is.EqualTo(_messageCount));
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        private volatile int _messageCount1;

        [Test]
        public async Task Unsubscribe()
        {
            _messageCount1 = 0;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { Interlocked.Increment(ref _messageCount1); }, cs.Token);
                await ipfs.PubSubApi.PublishAsync(topic, "hello world!", default);
                await Task.Delay(100, default);
                Assert.That(_messageCount1, Is.EqualTo(1));

                cs.Cancel();
                await ipfs.PubSubApi.PublishAsync(topic, "hello world!!!", default);
                await Task.Delay(100, default);
                Assert.That(_messageCount1, Is.EqualTo(1));
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [Test]
        public async Task Subscribe_BinaryMessage()
        {
            var messages = new List<IPublishedMessage>();
            var expected = new byte[] {0, 1, 2, 4, (byte) 'a', (byte) 'b', 0xfe, 0xff};
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { messages.Add(msg); }, cs.Token);
                await ipfs.PubSubApi.PublishAsync(topic, expected, cs.Token);

                await Task.Delay(100, cs.Token);
                Assert.That(messages.Count, Is.EqualTo(1));
                Assert.That(expected, Is.EqualTo(messages[0].DataBytes));
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        [Test]
        public async Task Subscribe_StreamMessage()
        {
            var messages = new List<IPublishedMessage>();
            var expected = new byte[] {0, 1, 2, 4, (byte) 'a', (byte) 'b', 0xfe, 0xff};
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { messages.Add(msg); }, cs.Token);
                var ms = new MemoryStream(expected, false);
                await ipfs.PubSubApi.PublishAsync(topic, ms, cs.Token);

                await Task.Delay(100, cs.Token);
                Assert.That(messages.Count, Is.EqualTo(1));
                Assert.That(expected, Is.EqualTo(messages[0].DataBytes));
            }
            finally
            {
                cs.Cancel();
                await ipfs.StopAsync();
            }
        }
    }
}
