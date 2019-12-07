using System;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Keystore;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Keystore;
using Catalyst.TestUtils;
using Lib.P2P;
using Lib.P2P.Cryptography;
using MultiFormats;
using MultiFormats.Registry;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    public class DfsTest
    {
        private IDfs ipfs;
        private IDfs ipfsOther;

        public DfsTest(ITestOutputHelper output)
        {
            var testFixture = new TestFixture(output);
            ipfs = testFixture.Ipfs;
            ipfsOther = testFixture.IpfsOther;
        }
        
        [Fact]
        public void Can_Create()
        {
            var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            var testPasswordManager = new PasswordManager(new TestPasswordReader(), new PasswordRegistry());
            var dfs = new Dfs(hashProvider, testPasswordManager);
            Assert.NotNull(dfs);
        }

        [Fact]
        public async Task Can_Dispose()
        {
            using (var node = new TempNode())
            {
                await node.StartAsync();
            }
        }

        // [Fact]
        // public async Task IpfsPass_Passphrase()
        // {
        //     var secret = "this is not a secure pass phrase";
        //     var ipfs = new Dfs();
        //     ipfs.Options = TestFixture.Ipfs.Options;
        //     await ipfs.KeyChainAsync();
        //
        //     Environment.SetEnvironmentVariable("IPFS_PASS", secret);
        //     try
        //     {
        //         ipfs = new Dfs();
        //         ipfs.Options = TestFixture.Ipfs.Options;
        //         await ipfs.KeyChainAsync();
        //     }
        //     finally
        //     {
        //         Environment.SetEnvironmentVariable("IPFS_PASS", null);
        //     }
        // }

        // [Fact]
        // public async Task Wrong_Passphrase()
        // {
        //     var ipfs = TestFixture.Ipfs;
        //     await ipfs.KeyChainAsync();
        //
        //     var ipfsOther = new Dfs()
        //     {
        //         Options = ipfs.Options
        //     };
        //     ExceptionAssert.Throws<UnauthorizedAccessException>(() =>
        //     {
        //         var _ = ipfsOther.KeyChainAsync().Result;
        //     });
        // }

        // [Fact]
        // [ExpectedException(typeof(Exception))]
        // public void IpfsPass_Missing()
        // {
        //     var _ = new Dfs();
        // }

        [Fact]
        public async Task Can_Start_And_Stop()
        {
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
            var peer1 = await ipfs.LocalPeer;
            var peer2 = await ipfsOther.LocalPeer;

            for (int n = 0; n < 3; ++n)
            {
                await ipfs.StartAsync();
                Assert.NotEqual(0, peer1.Addresses.Count());
                await ipfsOther.StartAsync();
                Assert.NotEqual(0, peer2.Addresses.Count());

                await ipfsOther.StopAsync();
                Assert.Equal(0, peer2.Addresses.Count());
                await ipfs.StopAsync();
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
            Task<Peer>[] tasks = new Task<Peer>[]
            {
                Task.Run(async () => await ipfs.LocalPeer),
                Task.Run(async () => await ipfs.LocalPeer)
            };
            var r = await Task.WhenAll(tasks);
            Assert.Equal(r[0], r[1]);
        }

        [Fact]
        public async Task KeyChain()
        {
            Task<IKeyApi>[] tasks = new Task<IKeyApi>[]
            {
                Task.Run(async () => await ipfs.KeyChainAsync()),
                Task.Run(async () => await ipfs.KeyChainAsync())
            };
            var r = await Task.WhenAll(tasks);
            Assert.Equal(r[0], r[1]);
        }

        [Fact]
        public async Task KeyChain_GetKey()
        {
            var keyChain = await ipfs.KeyChainAsync();
            var key = await keyChain.GetPrivateKeyAsync("self");
            Assert.NotNull(key);
            Assert.True(key.IsPrivate);
        }

        [Fact]
        public async Task Swarm_Gets_Bootstrap_Peers()
        {
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
                await ipfs.StopAsync();
            }
        }

        [Fact]
        public async Task Start_NoListeners()
        {
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
