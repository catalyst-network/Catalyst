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
using Lib.P2P.Multiplex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests.Multiplex
{
    [TestClass]
    public class HeaderTest
    {
        [TestMethod]
        public void StreamIds()
        {
            Roundtrip(0, PacketType.NewStream);
            Roundtrip(1, PacketType.NewStream);
            Roundtrip(0x1234, PacketType.NewStream);
            Roundtrip(0x12345678, PacketType.NewStream);
            Roundtrip(Header.MinStreamId, PacketType.NewStream);
            Roundtrip(Header.MaxStreamId, PacketType.NewStream);
        }

        private void Roundtrip(long id, PacketType type)
        {
            var header1 = new Header {StreamId = id, PacketType = type};
            var ms = new MemoryStream();
            header1.WriteAsync(ms).Wait();
            ms.Position = 0;
            var header2 = Header.ReadAsync(ms).Result;
            Assert.AreEqual(header1.StreamId, header2.StreamId);
            Assert.AreEqual(header1.PacketType, header2.PacketType);
        }
    }
}
