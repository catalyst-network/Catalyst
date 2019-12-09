using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Lib.P2P;
using MultiFormats;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class DhtApiTest
    {
        private IDfsService ipfs;
        private Peer _peer;
        private MultiHash _locaId;

        public DhtApiTest(ITestOutputHelper output)
        {
            ipfs = TestDfs.GetTestDfs(output);
            
            _locaId = ipfs.LocalPeer.ConfigureAwait(false).GetAwaiter().GetResult().Id;
        }

        [Fact]
        public async Task Local_Info()
        {
            var peer = ipfs.DhtApi.FindPeerAsync(_locaId).GetAwaiter().GetResult();
            Assert.IsType(typeof(Peer), peer);
            Assert.NotNull(peer.Addresses);
            Assert.StartsWith("net-ipfs/", peer.AgentVersion);
            Assert.NotNull(peer.Id);
            Assert.StartsWith("ipfs/", peer.ProtocolVersion);
            Assert.NotNull(peer.PublicKey);
            Assert.Equal(_locaId, _peer.Id);
            Assert.NotNull(_peer.Addresses);
            Assert.True(_peer.IsValid());
            Assert.True(peer.IsValid());
        }

        [Fact]
        public async Task Mars_Info()
        {
            var marsId = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
            var marsAddr = $"/ip6/::1/p2p/{marsId}";
            var swarm = await ipfs.SwarmService;
            var mars = swarm.RegisterPeerAddress(marsAddr);

            var peer = ipfs.DhtApi.FindPeerAsync(marsId).GetAwaiter().GetResult();
            Assert.Equal(mars.Id, peer.Id);
            Assert.Equal(mars.Addresses.First(), peer.Addresses.First());
        }

        // [Fact]
        // [Ignore("https://github.com/richardschneider/net-ipfs-engine/issues/74#issuecomment-500668261")]
        // public async Task Mars_Info()
        // {
        //     var marsId = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        //     var ipfs = TestFixture.Ipfs;
        //     await ipfs.StartAsync();
        //     try
        //     {
        //         var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        //         var mars = await ipfs.Dht.FindPeerAsync(marsId, cts.Token);
        //         Assert.Equal(marsId, mars.Id);
        //         Assert.NotNull(mars.Addresses);
        //         Assert.True(mars.IsValid());
        //     }
        //     finally
        //     {
        //         await ipfs.StopAsync();
        //     }
        // }

        // [Fact]
        // [Ignore("https://github.com/richardschneider/net-ipfs-engine/issues/74")]
        // public async Task FindProvider()
        // {
        //     var folder = "QmS4ustL54uo8FzR9455qaxZwuMiUhyvMcX9Ba8nUH4uVv";
        //     var ipfs = TestFixture.Ipfs;
        //     await ipfs.StartAsync();
        //     try
        //     {
        //         var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        //         var providers = await ipfs.Dht.FindProvidersAsync(folder, 1, null, cts.Token);
        //         Assert.Equal(1, providers.Count());
        //     }
        //     finally
        //     {
        //         await ipfs.StopAsync();
        //     }
        // }
    }
}
