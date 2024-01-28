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
using System.Net.Sockets;
using System.Threading.Tasks;
using MultiFormats;

namespace Lib.P2P.Tests
{
    public class MultiAddressExtensionsTest
    {
        [Test]
        public void Cloning()
        {
            var a = new MultiAddress("/dns/libp2p.io/tcp/5001");
            var b = a.Clone();
            Assert.Equals(a, b);
            Assert.That(a.Protocols, Is.Not.EqualTo(b.Protocols));
        }

        [Test]
        public async Task Resolving()
        {
            var local = new MultiAddress("/ip4/127.0.0.1/tcp/5001");
            var r0 = await local.ResolveAsync();
            Assert.Equals(1, r0.Count);
            Assert.Equals(local, r0[0]);
        }

        [Test]
        public async Task Resolving_Dns()
        {
            var dns = await new MultiAddress("/dns/libp2p.io/tcp/5001").ResolveAsync();
            Assert.That(0, Is.Not.EqualTo(dns.Count));
            var dns4 = await new MultiAddress("/dns4/libp2p.io/tcp/5001").ResolveAsync();
            var dns6 = await new MultiAddress("/dns6/libp2p.io/tcp/5001").ResolveAsync();
            Assert.Equals(dns.Count, dns4.Count + dns6.Count);
        }

        [Test]
        public async Task Resolving_HTTP()
        {
            var r = await new MultiAddress("/ip4/127.0.0.1/http").ResolveAsync();
            Assert.Equals("/ip4/127.0.0.1/http/tcp/80", r.First());

            r = await new MultiAddress("/ip4/127.0.0.1/http/tcp/8080").ResolveAsync();
            Assert.Equals("/ip4/127.0.0.1/http/tcp/8080", r.First());
        }

        [Test]
        public async Task Resolving_HTTPS()
        {
            var r = await new MultiAddress("/ip4/127.0.0.1/https").ResolveAsync();
            Assert.Equals("/ip4/127.0.0.1/https/tcp/443", r.First());

            r = await new MultiAddress("/ip4/127.0.0.1/https/tcp/4433").ResolveAsync();
            Assert.Equals("/ip4/127.0.0.1/https/tcp/4433", r.First());
        }

        [Test]
        public void Resolving_Unknown()
        {
            ExceptionAssert.Throws<SocketException>(() =>
            {
                var _ = new MultiAddress("/dns/does.not.exist/tcp/5001")
                   .ResolveAsync()
                   .Result;
            });
        }
    }
}
