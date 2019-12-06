using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper.Configuration.Annotations;
using Lib.P2P;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class DhtApiTest
    {
        [Fact]
        public async Task Local_Info()
        {
            var ipfs = TestFixture.Ipfs;
            var locaId = (await ipfs.LocalPeer).Id;
            var peer = await ipfs.Dht.FindPeerAsync(locaId);

            Assert.IsType(typeof(Peer), peer);
            Assert.Equal(locaId, peer.Id);
            Assert.NotNull(peer.Addresses);
            Assert.True(peer.IsValid());
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
