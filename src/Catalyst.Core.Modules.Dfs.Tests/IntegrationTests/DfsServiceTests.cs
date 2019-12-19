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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Catalyst.Core.Modules.Hashing;
using Catalyst.TestUtils;
using FluentAssertions;
using Lib.P2P;
using Lib.P2P.Cryptography;
using MultiFormats;
using MultiFormats.Registry;
using Nethermind.HashLib;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests
{
    public sealed class DfsServiceTests : FileSystemBasedTest
    {
        private readonly IDfsService _dfs1;
        private readonly IDfsService _dfs2;
        private readonly ILogger _logger;
        private readonly ITestOutputHelper _output;
        private readonly IHashProvider _hashProvider;

        public DfsServiceTests(ITestOutputHelper output) : base(output)
        {
            _hashProvider = this.ContainerProvider.Container.Resolve<IHashProvider>();

            _output = output;
            var passwordReader = Substitute.For<IPasswordManager>();
            passwordReader.RetrieveOrPromptAndAddPasswordToRegistry(Arg.Any<PasswordRegistryTypes>(), Arg.Any<string>())
               .Returns(TestPasswordReader.BuildSecureStringPassword("abcd"));

            _logger = Substitute.For<ILogger>();

            _dfs1 = TestDfs.GetTestDfs(output);
            _dfs2 = TestDfs.GetTestDfs(output);
            
            // Starting IPFS takes a few seconds.  Do it here, so that individual
            // test times are not affected.
            // _dfs1.GenericApi.IdAsync()
            //    .ConfigureAwait(false)
            //    .GetAwaiter()
            //    .GetResult();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task DFS_should_add_and_read_text()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            const string text = "good morning";

            var id = await _dfs1.UnixFsApi.AddTextAsync(text, cancel: cts.Token);
            var content = await _dfs1.UnixFsApi?.ReadAllTextAsync(id.Id, cts.Token);

            content.Should().Be(text);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task DFS_should_add_and_read_binary()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var binary = new byte[]
            {
                1, 2, 3
            };
            var ms = new MemoryStream(binary);

            var id = await _dfs1.UnixFsApi.AddAsync(ms, "", cancel: cts.Token);
            using (var stream = await _dfs1.UnixFsApi.ReadFileAsync(id.Id, cts.Token))
            {
                var content = new byte[binary.Length];
                await stream.ReadAsync(content, 0, content.Length, cts.Token);
                content.Should().Equal(binary);
            }
        }

        [Fact(Skip = "waiting for Dns seed fix: https://github.com/catalyst-network/Catalyst.Framework/issues/1075")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task DFS_should_connect_to_a_seednode()
        {
            var seeds = (await _dfs1.BootstrapApi.ListAsync().ConfigureAwait(false))
               .Select(a => a.PeerId)
               .ToArray();
            Assert.True(seeds.Length > 0, "no seed nodes defined");

            // Wait for a connection to a seed node.
            var start = DateTime.Now;
            var end = DateTime.Now.AddSeconds(15);
            var found = false;
            while (!found)
            {
                Assert.True(DateTime.Now <= end, "timeout");
                var peers = await _dfs1.SwarmApi.PeersAsync().ConfigureAwait(false);
                found = peers.Any(p => seeds.Contains(p.Id));
                await Task.Delay(100).ConfigureAwait(false);
            }

            _output.WriteLine(
                $"Found in {(DateTime.Now - start).TotalSeconds.ToString(CultureInfo.InvariantCulture)} seconds.");
        }

        [Fact]
        public async Task Can_Dispose()
        {
            using var node = TestDfs.GetTestDfs(_output);
            await node.StartAsync();
        }
        
        [Fact]
        public async Task Can_Start_And_Stop()
        {
            var peer = _dfs1.LocalPeer;

            Assert.False(_dfs1.IsStarted);
            await _dfs1.StartAsync();
            Assert.True(_dfs1.IsStarted);
            Assert.NotEqual(0, peer.Addresses.Count());
            await _dfs1.StopAsync();
            Assert.False(_dfs1.IsStarted);
            Assert.Equal(0, peer.Addresses.Count());

            await _dfs1.StartAsync();
            Assert.NotEqual(0, peer.Addresses.Count());
            await _dfs1.StopAsync();
            Assert.Equal(0, peer.Addresses.Count());

            await _dfs1.StartAsync();
            Assert.NotEqual(0, peer.Addresses.Count());
            ExceptionAssert.Throws<Exception>(() => _dfs1.StartAsync().Wait());
            await _dfs1.StopAsync();
            Assert.Equal(0, peer.Addresses.Count());
        }

        [Fact]
        public async Task Can_Start_And_Stop_MultipleEngines()
        {
            var peer1 = _dfs1.LocalPeer;
            var peer2 = _dfs2.LocalPeer;

            for (int n = 0; n < 3; ++n)
            {
                await _dfs1.StartAsync();
                Assert.NotEqual(0, peer1.Addresses.Count());
                await _dfs2.StartAsync();
                Assert.NotEqual(0, peer2.Addresses.Count());

                await _dfs2.StopAsync();
                Assert.Equal(0, peer2.Addresses.Count());
                await _dfs1.StopAsync();
                Assert.Equal(0, peer1.Addresses.Count());
            }
        }

        [Fact]
        public async Task Can_Use_Private_Node()
        {
            using (var ipfs = TestDfs.GetTestDfs(_output))
            {
                ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
                ipfs.Options.Swarm.PrivateNetworkKey = new PreSharedKey().Generate();
                await _dfs1.StartAsync();
            }
        }

        [Fact]
        public async Task LocalPeer()
        {
            Task<Peer>[] tasks = new Task<Peer>[]
            {
                Task.Run(async () => _dfs1.LocalPeer),
                Task.Run(async () => _dfs1.LocalPeer)
            };
            var r = await Task.WhenAll(tasks);
            Assert.Equal(r[0], r[1]);
        }

        [Fact]
        public async Task KeyChain()
        {
            Task<IKeyApi>[] tasks = new Task<IKeyApi>[]
            {
                Task.Run(async () => _dfs1.KeyApi),
                Task.Run(async () => _dfs1.KeyApi)
            };
            var r = await Task.WhenAll(tasks);
            Assert.Equal(r[0], r[1]);
        }

        [Fact]
        public async Task KeyChain_GetKey()
        {
            var keyChain = await _dfs1.KeyChainAsync();
            var key = await keyChain.GetPrivateKeyAsync("self");
            Assert.NotNull(key);
            Assert.True(key.IsPrivate);
        }

        [Fact]
        public async Task Swarm_Gets_Bootstrap_Peers()
        {
            var bootPeers = (await _dfs1.BootstrapApi.ListAsync()).ToArray();
            await _dfs1.StartAsync();
            try
            {
                var swarm = _dfs1.SwarmService;
                var knownPeers = swarm.KnownPeerAddresses.ToArray();
                var endTime = DateTime.Now.AddSeconds(3);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        throw new XunitException("Bootstrap peers are not known.");
                    }

                    if (bootPeers.All(a => knownPeers.Contains(a)))
                    {
                        break;
                    }

                    await Task.Delay(50);
                    knownPeers = swarm.KnownPeerAddresses.ToArray();
                }
            }
            finally
            {
                await _dfs1.StopAsync();
            }
        }

        [Fact]
        public async Task Start_NoListeners()
        {
            var swarm = await _dfs1.ConfigApi.GetAsync("Addresses.Swarm");
            try
            {
                await _dfs1.ConfigApi.SetAsync("Addresses.Swarm", "[]");
                await _dfs1.StartAsync();
            }
            finally
            {
                await _dfs1.StopAsync();
                await _dfs1.ConfigApi.SetAsync("Addresses.Swarm", swarm);
            }
        }

        [Fact]
        public async Task Start_InvalidListener()
        {
            var swarm = await _dfs1.ConfigApi.GetAsync("Addresses.Swarm");
            try
            {
                // 1 - missing ip address
                // 2 - invalid protocol name
                // 3 - okay
                var values = JToken.Parse("['/tcp/0', '/foo/bar', '/ip4/0.0.0.0/tcp/0']");
                await _dfs1.ConfigApi.SetAsync("Addresses.Swarm", values);
                await _dfs1.StartAsync();
            }
            finally
            {
                await _dfs1.StopAsync();
                await _dfs1.ConfigApi.SetAsync("Addresses.Swarm", swarm);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dfs1.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
