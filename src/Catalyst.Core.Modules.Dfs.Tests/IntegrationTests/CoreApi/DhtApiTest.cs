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
using Catalyst.Abstractions.Dfs;
using Catalyst.TestUtils;
using Lib.P2P;
using MultiFormats;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class DhtApiTest
    {
        private IDfsService ipfs;
        private MultiHash _locaId;

        public DhtApiTest()
        {
            ipfs = TestDfs.GetTestDfs();
            
            _locaId = ipfs.LocalPeer.Id;
        }

        [Test]
        public void Local_Info()
        {
            var peer = ipfs.DhtApi.FindPeerAsync(_locaId).GetAwaiter().GetResult();
            Assert.IsInstanceOf(typeof(Peer), peer);
            Assert.NotNull(peer.Addresses);
            Assert.That(peer.AgentVersion, Does.StartWith("net-ipfs/"));
            Assert.NotNull(peer.Id);
            Assert.That(peer.ProtocolVersion, Does.StartWith("ipfs/"));
            Assert.NotNull(peer.PublicKey);
            Assert.AreEqual(_locaId, peer.Id);
            Assert.True(peer.IsValid());
        }

        [Test]
        public void Mars_Info()
        {
            const string marsId = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
            var marsAddr = $"/ip6/::1/p2p/{marsId}";
            var swarm = ipfs.SwarmService;
            var mars = swarm.RegisterPeerAddress(marsAddr);

            var peer = ipfs.DhtApi.FindPeerAsync(marsId).GetAwaiter().GetResult();
            Assert.AreEqual(mars.Id, peer.Id);
            Assert.AreEqual(mars.Addresses.First(), peer.Addresses.First());
        }

        // [Test]
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
        //         Assert.AreEqual(marsId, mars.Id);
        //         Assert.NotNull(mars.Addresses);
        //         Assert.True(mars.IsValid());
        //     }
        //     finally
        //     {
        //         await ipfs.StopAsync();
        //     }
        // }

        // [Test]
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
        //         Assert.AreEqual(1, providers.Count());
        //     }
        //     finally
        //     {
        //         await ipfs.StopAsync();
        //     }
        // }
    }
}
