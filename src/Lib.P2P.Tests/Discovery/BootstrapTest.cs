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
using Lib.P2P.Discovery;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiFormats;

namespace Lib.P2P.Tests.Discovery
{
    [TestClass]
    public class BootstrapTest
    {
        [TestMethod]
        public async Task NullList()
        {
            var bootstrap = new Bootstrap {Addresses = null};
            var found = 0;
            bootstrap.PeerDiscovered += (s, e) => { ++found; };
            await bootstrap.StartAsync();
            Assert.AreEqual(0, found);
        }

        [TestMethod]
        public async Task Discovered()
        {
            var bootstrap = new Bootstrap
            {
                Addresses = new MultiAddress[]
                {
                    "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ",
                    "/ip4/104.131.131.83/tcp/4001/p2p/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
                }
            };
            var found = 0;
            bootstrap.PeerDiscovered += (s, peer) =>
            {
                Assert.IsNotNull(peer);
                Assert.AreEqual("QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ", peer.Id.ToBase58());
                CollectionAssert.AreEqual(bootstrap.Addresses.ToArray(), peer.Addresses.ToArray());
                ++found;
            };
            await bootstrap.StartAsync();
            Assert.AreEqual(1, found);
        }

        [TestMethod]
        public async Task Discovered_Multiple_Peers()
        {
            var bootstrap = new Bootstrap
            {
                Addresses = new MultiAddress[]
                {
                    "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ",
                    "/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                    "/ip4/104.131.131.83/tcp/4001/p2p/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ",
                    "/ip6/::/tcp/4001/p2p/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h"
                }
            };
            var found = 0;
            bootstrap.PeerDiscovered += (s, peer) =>
            {
                Assert.IsNotNull(peer);
                ++found;
            };
            await bootstrap.StartAsync();
            Assert.AreEqual(2, found);
        }

        [TestMethod]
        public async Task Stop_Removes_EventHandlers()
        {
            var bootstrap = new Bootstrap
            {
                Addresses = new MultiAddress[]
                {
                    "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
                }
            };
            var found = 0;
            bootstrap.PeerDiscovered += (s, e) =>
            {
                Assert.IsNotNull(e);
                ++found;
            };
            await bootstrap.StartAsync();
            Assert.AreEqual(1, found);
            await bootstrap.StopAsync();

            await bootstrap.StartAsync();
            Assert.AreEqual(1, found);
        }

        [TestMethod]
        public async Task Missing_ID_Is_Ignored()
        {
            var bootstrap = new Bootstrap
            {
                Addresses = new MultiAddress[]
                {
                    "/ip4/104.131.131.82/tcp/4002",
                    "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
                }
            };
            var found = 0;
            bootstrap.PeerDiscovered += (s, e) =>
            {
                Assert.IsNotNull(e);
                Assert.IsNotNull(e.Addresses);
                Assert.AreEqual(bootstrap.Addresses.Last(), e.Addresses.First());
                ++found;
            };
            await bootstrap.StartAsync();
            Assert.AreEqual(1, found);
        }
    }
}
