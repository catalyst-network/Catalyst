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
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using FluentAssertions;
using Lib.P2P.PubSub;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class PubSubApiTest
    {
        private IDfsService ipfs;

        public PubSubApiTest(ITestOutputHelper output)
        {
            ipfs = TestDfs.GetTestDfs(output);
        }
        
        [Fact]
        public void Api_Exists()
        {
            Assert.NotNull(ipfs.PubSubApi);
        }

        [Fact]
        public void Peers_Unknown_Topic()
        {
            var topic = "net-ipfs-http-client-test-unknown" + Guid.NewGuid().ToString();
            var peers = ipfs.PubSubApi.PeersAsync(topic).Result.ToArray();
            Assert.Equal(0, peers.Length);
        }

        [Fact]
        public async Task Subscribed_Topics()
        {
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { }, cs.Token);
                var topics = ipfs.PubSubApi.SubscribedTopicsAsync().Result.ToArray();
                Assert.True(topics.Length > 0);
                topics.Should().Contain(topic);
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
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { Interlocked.Increment(ref messageCount); }, cs.Token);
                await ipfs.PubSubApi.PublishAsync(topic, "hello world!");

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
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { Interlocked.Increment(ref messageCount); }, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSubApi.PublishAsync(topic, msg);
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
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();

            Action<IPublishedMessage> processMessage = (msg) => { Interlocked.Increment(ref messageCount); };
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, processMessage, cs.Token);
                await ipfs.PubSubApi.SubscribeAsync(topic, processMessage, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSubApi.PublishAsync(topic, msg);
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
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { Interlocked.Increment(ref messageCount1); }, cs.Token);
                await ipfs.PubSubApi.PublishAsync(topic, "hello world!");
                await Task.Delay(100);
                Assert.Equal(1, messageCount1);

                cs.Cancel();
                await ipfs.PubSubApi.PublishAsync(topic, "hello world!!!");
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
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { messages.Add(msg); }, cs.Token);
                await ipfs.PubSubApi.PublishAsync(topic, expected);

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
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSubApi.SubscribeAsync(topic, msg => { messages.Add(msg); }, cs.Token);
                var ms = new MemoryStream(expected, false);
                await ipfs.PubSubApi.PublishAsync(topic, ms);

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
