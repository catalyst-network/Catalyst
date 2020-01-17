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
        private MultiHash _locaId;

        public DhtApiTest(ITestOutputHelper output)
        {
            ipfs = TestDfs.GetTestDfs(output);
            
            _locaId = ipfs.LocalPeer.Id;
        }

        [Fact]
        public void Local_Info()
        {
            var peer = ipfs.DhtApi.FindPeerAsync(_locaId).GetAwaiter().GetResult();
            Assert.IsType(typeof(Peer), peer);
            Assert.NotNull(peer.Addresses);
            Assert.StartsWith("net-ipfs/", peer.AgentVersion);
            Assert.NotNull(peer.Id);
            Assert.StartsWith("ipfs/", peer.ProtocolVersion);
            Assert.NotNull(peer.PublicKey);
            Assert.Equal(_locaId, peer.Id);
            Assert.True(peer.IsValid());
        }

        [Fact]
        public void Mars_Info()
        {
            const string marsId = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
            var marsAddr = $"/ip6/::1/p2p/{marsId}";
            var swarm = ipfs.SwarmService;
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
