﻿using System;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Catalyst.Core.Modules.Keystore;
using Lib.P2P;
using Lib.P2P.Cryptography;
using MultiFormats;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    [TestClass]
    public class IpfsEngineTest
    {
        [Fact]
        public void Can_Create()
        {
            var ipfs = new IpfsEngine("this is not a secure pass phrase".ToCharArray());
            Assert.NotNull(ipfs);
        }

        [Fact]
        public async Task Can_Dispose()
        {
            using (var node = new TempNode())
            {
                await node.StartAsync();
            }
        }

        [Fact]
        public async Task SecureString_Passphrase()
        {
            var secret = "this is not a secure pass phrase";
            var ipfs = new IpfsEngine(secret.ToCharArray());
            ipfs.Options = TestFixture.Ipfs.Options;
            await ipfs.KeyChainAsync();

            var passphrase = new SecureString();
            foreach (var c in secret) passphrase.AppendChar(c);
            ipfs = new IpfsEngine(passphrase);
            ipfs.Options = TestFixture.Ipfs.Options;
            await ipfs.KeyChainAsync();
        }

        [Fact]
        public async Task IpfsPass_Passphrase()
        {
            var secret = "this is not a secure pass phrase";
            var ipfs = new IpfsEngine(secret.ToCharArray());
            ipfs.Options = TestFixture.Ipfs.Options;
            await ipfs.KeyChainAsync();

            Environment.SetEnvironmentVariable("IPFS_PASS", secret);
            try
            {
                ipfs = new IpfsEngine();
                ipfs.Options = TestFixture.Ipfs.Options;
                await ipfs.KeyChainAsync();
            }
            finally
            {
                Environment.SetEnvironmentVariable("IPFS_PASS", null);
            }
        }

        [Fact]
        public async Task Wrong_Passphrase()
        {
            var ipfs1 = TestFixture.Ipfs;
            await ipfs1.KeyChainAsync();

            var ipfs2 = new IpfsEngine("the wrong pass phrase".ToCharArray())
            {
                Options = ipfs1.Options
            };
            ExceptionAssert.Throws<UnauthorizedAccessException>(() =>
            {
                var _ = ipfs2.KeyChainAsync().Result;
            });
        }

        [Fact]
        [ExpectedException(typeof(Exception))]
        public void IpfsPass_Missing()
        {
            var _ = new IpfsEngine();
        }

        [Fact]
        public async Task Can_Start_And_Stop()
        {
            var ipfs = TestFixture.Ipfs;
            var peer = await ipfs.LocalPeer;

            Assert.False(ipfs.IsStarted);
            await ipfs.StartAsync();
            Assert.True(ipfs.IsStarted);
            Assert.NotEqual(0, peer.Addresses.Count());
            await ipfs.StopAsync();
            Assert.False(ipfs.IsStarted);
            Assert.Equal(0, peer.Addresses.Count());

            await ipfs.StartAsync();
            Assert.NotEqual(0, peer.Addresses.Count());
            await ipfs.StopAsync();
            Assert.Equal(0, peer.Addresses.Count());

            await ipfs.StartAsync();
            Assert.NotEqual(0, peer.Addresses.Count());
            ExceptionAssert.Throws<Exception>(() => ipfs.StartAsync().Wait());
            await ipfs.StopAsync();
            Assert.Equal(0, peer.Addresses.Count());
        }

        [Fact]
        public async Task Can_Start_And_Stop_MultipleEngines()
        {
            var ipfs1 = TestFixture.Ipfs;
            var ipfs2 = TestFixture.IpfsOther;
            var peer1 = await ipfs1.LocalPeer;
            var peer2 = await ipfs2.LocalPeer;

            for (int n = 0; n < 3; ++n)
            {
                await ipfs1.StartAsync();
                Assert.NotEqual(0, peer1.Addresses.Count());
                await ipfs2.StartAsync();
                Assert.NotEqual(0, peer2.Addresses.Count());

                await ipfs2.StopAsync();
                Assert.Equal(0, peer2.Addresses.Count());
                await ipfs1.StopAsync();
                Assert.Equal(0, peer1.Addresses.Count());
            }
        }

        [Fact]
        public async Task Can_Use_Private_Node()
        {
            using (var ipfs = new TempNode())
            {
                ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
                ipfs.Options.Swarm.PrivateNetworkKey = new PreSharedKey().Generate();
                await ipfs.StartAsync();
            }
        }

        [Fact]
        public async Task LocalPeer()
        {
            var ipfs = TestFixture.Ipfs;
            Task<Peer>[] tasks = new Task<Peer>[]
            {
                Task.Run(async () => await ipfs.LocalPeer),
                Task.Run(async () => await ipfs.LocalPeer)
            };
            var r = await Task.WhenAll(tasks);
            Assert.AreSame(r[0], r[1]);
        }

        [Fact]
        public async Task KeyChain()
        {
            var ipfs = TestFixture.Ipfs;
            Task<KeyChain>[] tasks = new Task<KeyChain>[]
            {
                Task.Run(async () => await ipfs.KeyChainAsync()),
                Task.Run(async () => await ipfs.KeyChainAsync())
            };
            var r = await Task.WhenAll(tasks);
            Assert.AreSame(r[0], r[1]);
        }

        [Fact]
        public async Task KeyChain_GetKey()
        {
            var ipfs = TestFixture.Ipfs;
            var keyChain = await ipfs.KeyChainAsync();
            var key = await keyChain.GetPrivateKeyAsync("self");
            Assert.NotNull(key);
            Assert.True(key.IsPrivate);
        }

        [Fact]
        public async Task Swarm_Gets_Bootstrap_Peers()
        {
            var ipfs = TestFixture.Ipfs;
            var bootPeers = (await ipfs.Bootstrap.ListAsync()).ToArray();
            await ipfs.StartAsync();
            try
            {
                var swarm = await ipfs.SwarmService;
                var knownPeers = swarm.KnownPeerAddresses.ToArray();
                var endTime = DateTime.Now.AddSeconds(3);
                while (true)
                {
                    if (DateTime.Now > endTime)
                        Assert.Fail("Bootstrap peers are not known.");
                    if (bootPeers.All(a => knownPeers.Contains(a)))
                        break;

                    await Task.Delay(50);
                    knownPeers = swarm.KnownPeerAddresses.ToArray();
                }
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [Fact]
        public async Task Start_NoListeners()
        {
            var ipfs = TestFixture.Ipfs;
            var swarm = await ipfs.Config.GetAsync("Addresses.Swarm");
            try
            {
                await ipfs.Config.SetAsync("Addresses.Swarm", "[]");
                await ipfs.StartAsync();
            }
            finally
            {
                await ipfs.StopAsync();
                await ipfs.Config.SetAsync("Addresses.Swarm", swarm);
            }
        }

        [Fact]
        public async Task Start_InvalidListener()
        {
            var ipfs = TestFixture.Ipfs;
            var swarm = await ipfs.Config.GetAsync("Addresses.Swarm");
            try
            {
                // 1 - missing ip address
                // 2 - invalid protocol name
                // 3 - okay
                var values = JToken.Parse("['/tcp/0', '/foo/bar', '/ip4/0.0.0.0/tcp/0']");
                await ipfs.Config.SetAsync("Addresses.Swarm", values);
                await ipfs.StartAsync();
            }
            finally
            {
                await ipfs.StopAsync();
                await ipfs.Config.SetAsync("Addresses.Swarm", swarm);
            }
        }
    }
}
