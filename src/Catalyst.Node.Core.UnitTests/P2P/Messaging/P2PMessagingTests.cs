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
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.UnitTest.TestUtils;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.P2P.Messaging
{
    public class P2PMessagingTests : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;
        private IEnumerable<IDisposable> _subscriptions;

        public P2PMessagingTests(ITestOutputHelper output) : base(output)
        {
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Peers_Can_Emit_And_Receive_Broadcast()
        {
            ConfigureContainerBuilder(_config);
            
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                var logger = container.Resolve<ILogger>();
                var certificateStore = container.Resolve<ICertificateStore>();

                var indexes = Enumerable.Range(0, 3).ToList();

                var peerSettings = indexes.Select(i => new PeerSettings(_config) { Port = 40100 + i }).ToList();
                var peers = peerSettings.Select(s => new P2PMessaging(s, certificateStore, logger)).ToList();

                var observers = indexes.Select(i => new AnyObserver(i, _output)).ToList();
                _subscriptions = peers.Select((p, i) => p.MessageStream.Subscribe(observers[i])).ToList();

                var broadcastMessage = TransactionHelper.GetTransaction().ToAny();
                await peers[0].BroadcastMessageAsync(broadcastMessage);

                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
                {
                    var tasks = peers
                       .Select(async p => await p.MessageStream.FirstAsync(a => !a.Equals(NullObjects.Any)))
                       .ToArray();
                    Task.WaitAll(tasks, cts.Token);
                }

                var received = observers.Select(o => o.Received).ToList();
                received.Count(r => r.TypeUrl == broadcastMessage.TypeUrl).Should().Be(3);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            if(_subscriptions == null) return;
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }
        }

        private class AnyObserver : IObserver<Any>
        {
            private readonly ITestOutputHelper _output;

            public AnyObserver(int index, ITestOutputHelper output)
            {
                _output = output;
                Index = index;
            }

            public Any Received { get; private set; }
            public int Index { get; }

            public void OnCompleted() { _output.WriteLine($"observer {Index} done"); }
            public void OnError(Exception error) { _output.WriteLine($"observer {Index} received error : {error.Message}"); }

            public void OnNext(Any value)
            {
                if(NullObjects.Any.Equals(value)) return;
                _output.WriteLine($"observer {Index} received message of type {value?.TypeUrl ?? "(null)"}");
                Received = value;
            }
        }
    }
}
