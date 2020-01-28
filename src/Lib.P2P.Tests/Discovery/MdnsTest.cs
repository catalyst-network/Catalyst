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
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.Discovery;
using Makaretu.Dns;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiFormats;

namespace Lib.P2P.Tests.Discovery
{
    [TestClass]
    public class MdnsTest
    {
        //Ignore test, contains race condition because of timing.
        [Ignore]
        [TestMethod]
        public async Task DiscoveryNext()
        {
            var serviceName = $"_{Guid.NewGuid()}._udp";
            var peer1 = new Peer
            {
                Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ",
                Addresses = new MultiAddress[]
                    {"/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"}
            };
            var peer2 = new Peer
            {
                Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuK",
                Addresses = new MultiAddress[]
                    {"/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuK"}
            };
            var done = new ManualResetEvent(false);
            var mdns1 = new MdnsNext
            {
                MulticastService = new MulticastService(),
                ServiceName = serviceName,
                LocalPeer = peer1
            };
            var mdns2 = new MdnsNext
            {
                MulticastService = new MulticastService(),
                ServiceName = serviceName,
                LocalPeer = peer2
            };
            mdns1.PeerDiscovered += (s, e) =>
            {
                if (e.Id == peer2.Id)
                    done.Set();
            };
            await mdns1.StartAsync();
            mdns1.MulticastService.Start();
            await mdns2.StartAsync();
            mdns2.MulticastService.Start();
            try
            {
                Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(2)), "timeout");
            }
            finally
            {
                await mdns1.StopAsync();
                await mdns2.StopAsync();
                mdns1.MulticastService.Stop();
                mdns2.MulticastService.Stop();
            }
        }

        //Ignore test, contains race condition because of timing.
        [Ignore]
        [TestMethod]
        public async Task DiscoveryJs()
        {
            var serviceName = $"_{Guid.NewGuid()}._udp";
            serviceName = "_foo._udp";
            var peer1 = new Peer
            {
                Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ",
                Addresses = new MultiAddress[]
                    {"/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"}
            };
            var peer2 = new Peer
            {
                Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuK",
                Addresses = new MultiAddress[]
                    {"/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuK"}
            };
            var done = new ManualResetEvent(false);
            var mdns1 = new MdnsJs
            {
                MulticastService = new MulticastService(),
                ServiceName = serviceName,
                LocalPeer = peer1
            };
            var mdns2 = new MdnsJs
            {
                MulticastService = new MulticastService(),
                ServiceName = serviceName,
                LocalPeer = peer2
            };
            mdns1.PeerDiscovered += (s, e) =>
            {
                if (e.Id == peer2.Id)
                    done.Set();
            };
            await mdns1.StartAsync();
            mdns1.MulticastService.Start();
            await mdns2.StartAsync();
            mdns2.MulticastService.Start();
            try
            {
                Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(2)), "timeout");
            }
            finally
            {
                await mdns1.StopAsync();
                await mdns2.StopAsync();
                mdns1.MulticastService.Stop();
                mdns2.MulticastService.Stop();
            }
        }

        [TestMethod]
        public void SafeDnsLabel()
        {
            Assert.AreEqual("a", MdnsNext.SafeLabel("a", 2));
            Assert.AreEqual("ab", MdnsNext.SafeLabel("ab", 2));
            Assert.AreEqual("ab.c", MdnsNext.SafeLabel("abc", 2));
            Assert.AreEqual("ab.cd", MdnsNext.SafeLabel("abcd", 2));
            Assert.AreEqual("ab.cd.e", MdnsNext.SafeLabel("abcde", 2));
            Assert.AreEqual("ab.cd.ef", MdnsNext.SafeLabel("abcdef", 2));
            Assert.AreEqual("ab.cd.ef.g", MdnsNext.SafeLabel("abcdefg", 2));
        }
    }
}
