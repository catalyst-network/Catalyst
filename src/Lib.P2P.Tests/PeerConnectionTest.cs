#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Lib.P2P.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests
{
    [TestClass]
    public class PeerConnectionTest
    {
        [TestMethod]
        public void Disposing()
        {
            var closeCount = 0;
            var stream = new MemoryStream();
            var connection = new PeerConnection {Stream = stream};
            connection.Closed += (s, e) => { ++closeCount; };
            Assert.IsTrue(connection.IsActive);
            Assert.IsNotNull(connection.Stream);

            connection.Dispose();
            Assert.IsFalse(connection.IsActive);
            Assert.IsNull(connection.Stream);

            // Can be disposed multiple times.
            connection.Dispose();

            Assert.IsFalse(connection.IsActive);
            Assert.AreEqual(1, closeCount);
        }

        [TestMethod]
        public void Stats()
        {
            var stream = new MemoryStream();
            var connection = new PeerConnection {Stream = stream};
            Assert.AreEqual(0, connection.BytesRead);
            Assert.AreEqual(0, connection.BytesWritten);

            var buffer = new byte[] {1, 2, 3};
            connection.Stream.Write(buffer, 0, 3);
            Assert.AreEqual(0, connection.BytesRead);
            Assert.AreEqual(3, connection.BytesWritten);

            stream.Position = 0;
            connection.Stream.ReadByte();
            connection.Stream.ReadByte();
            Assert.AreEqual(2, connection.BytesRead);
            Assert.AreEqual(3, connection.BytesWritten);
        }

        [TestMethod]
        public void Protocols()
        {
            var connection = new PeerConnection();
            Assert.AreEqual(0, connection.Protocols.Count);

            connection.AddProtocol(new Identify1());
            Assert.AreEqual(1, connection.Protocols.Count);

            connection.AddProtocols(new IPeerProtocol[] {new Mplex67(), new Plaintext1()});
            Assert.AreEqual(3, connection.Protocols.Count);
        }

        [TestMethod]
        public void CreatesOneStatsStream()
        {
            var a = new MemoryStream();
            var b = new MemoryStream();
            var connection = new PeerConnection();
            Assert.AreEqual(null, connection.Stream);

            connection.Stream = a;
            Assert.AreNotSame(a, connection.Stream);

            connection.Stream = b;
            Assert.AreSame(b, connection.Stream);
        }
    }
}
