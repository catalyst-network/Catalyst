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

using System.IO;
using System.Threading.Tasks;
using Lib.P2P.Protocols;

namespace Lib.P2P.Tests.Protocols
{
    public class MessageTest
    {
        [Test]
        public async Task Encoding()
        {
            var ms = new MemoryStream();
            await Message.WriteAsync("a", ms);
            var buf = ms.ToArray();
            Assert.That(buf.Length, Is.EqualTo(3));
            Assert.That(buf[0], Is.EqualTo(2));
            Assert.That(buf[1], Is.EqualTo((byte)'a'));
            Assert.That(buf[2], Is.EqualTo((byte)'\n'));
        }

        [Test]
        public async Task RoundTrip()
        {
            var msg = "/foobar/0.42.0";
            var ms = new MemoryStream();
            await Message.WriteAsync(msg, ms);
            ms.Position = 0;
            var result = await Message.ReadStringAsync(ms);
            Assert.That(msg, Is.EqualTo(result));
        }
    }
}
