/*
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.UnitTest.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.P2P.Messaging
{
    public class P2PMessagingTests : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;
        private IEnumerable<IDisposable> _subscriptions;
        private ILifetimeScope _scope;
        private ILogger _logger;
        private ICertificateStore _certificateStore;

        public P2PMessagingTests(ITestOutputHelper output) : base(output)
        {
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
        }

        private void ConfigureTestContainer()
        {
            WriteLogsToFile = false;
            WriteLogsToTestOutput = false;

            ConfigureContainerBuilder(_config);

            var container = ContainerBuilder.Build();
            _scope = container.BeginLifetimeScope(_currentTestName);

            _logger = container.Resolve<ILogger>();
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory.AddProvider(new SerilogLoggerProvider(_logger));

            _certificateStore = container.Resolve<ICertificateStore>();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Peers_Can_Emit_And_Receive_Broadcast()
        {
            ConfigureTestContainer();
            var indexes = Enumerable.Range(0, 3).ToList();

            var peerSettings = indexes.Select(i => new PeerSettings(_config) { Port = 40100 + i }).ToList();
            var peers = peerSettings.Select(s => new P2PMessaging(s, _certificateStore, _logger)).ToList();

            var observers = indexes.Select(i => new AnyMessageObserver(i, _logger)).ToList();
            _subscriptions = peers.Select((p, i) => p.MessageStream.Subscribe(observers[i])).ToList();

            var broadcastMessage = TransactionHelper.GetTransaction().ToAny();
            var context = Substitute.For<IChannelHandlerContext>();
            await peers[0].BroadcastMessageAsync(broadcastMessage);

            var tasks = peers
               .Select(async p => await p.MessageStream.FirstAsync(a => a != NullObjects.ChanneledAny))
               .ToArray();
            Task.WaitAll(tasks, TimeSpan.FromMilliseconds(100));
        
            var received = observers.Select(o => o.Received).ToList();
            received.Count(r => r.Payload.TypeUrl == broadcastMessage.TypeUrl).Should().Be(3);
        
        }

        [Fact(Skip = "not ready yet")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Peer_Can_Ping_Other_Peer_And_Receive_Pong()
        {
            ConfigureTestContainer();

            var indexes = Enumerable.Range(0, 3).ToList();

            var peerSettings = indexes.Select(i => new PeerSettings(_config) { Port = 40100 + i }).ToList();
            var peers = peerSettings.Select(s => new P2PMessaging(s, _certificateStore, _logger)).ToList();

            //var observers = indexes.Select(i => new AnyObserver(i, _logger)).ToList();
            //_subscriptions = peers.Select((p, i) => p.MessageStream.Subscribe(observers[i])).ToList();

            //await peers[0].PingAsync(peers[2].Identifier);

            //var tasks = Task.Run(() => peers[2].MessageStream.FirstAsync(a => !a.Equals(NullObjects.Any)));
            //Task.WaitAll(new Task[]{tasks}, TimeSpan.FromMilliseconds(200))};
            
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            _scope?.Dispose();
            if(_subscriptions == null) return;
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }
        }
    }
}
