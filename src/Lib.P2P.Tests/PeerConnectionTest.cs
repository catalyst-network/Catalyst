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
using Lib.P2P.Protocols;

namespace Lib.P2P.Tests
{
    public class PeerConnectionTest
    {
        [Test]
        public void Disposing()
        {
            var closeCount = 0;
            var stream = new MemoryStream();
            var connection = new PeerConnection {Stream = stream};
            connection.Closed += (s, e) => { ++closeCount; };
            Assert.That(connection.IsActive, Is.True);
            Assert.That(connection.Stream, Is.Not.Null);

            connection.Dispose();
            Assert.That(connection.IsActive, Is.False);
            Assert.That(connection.Stream, Is.Null);

            // Can be disposed multiple times.
            connection.Dispose();

            Assert.That(connection.IsActive, Is.False);
            Assert.Equals(1, closeCount);
        }

        [Test]
        public void Stats()
        {
            var stream = new MemoryStream();
            var connection = new PeerConnection {Stream = stream};
            Assert.Equals(0, connection.BytesRead);
            Assert.Equals(0, connection.BytesWritten);

            var buffer = new byte[] {1, 2, 3};
            connection.Stream.Write(buffer, 0, 3);
            Assert.Equals(0, connection.BytesRead);
            Assert.Equals(3, connection.BytesWritten);

            stream.Position = 0;
            connection.Stream.ReadByte();
            connection.Stream.ReadByte();
            Assert.Equals(2, connection.BytesRead);
            Assert.Equals(3, connection.BytesWritten);
        }

        [Test]
        public void Protocols()
        {
            var connection = new PeerConnection();
            Assert.Equals(0, connection.Protocols.Count);

            connection.AddProtocol(new Identify1());
            Assert.Equals(1, connection.Protocols.Count);

            connection.AddProtocols(new IPeerProtocol[] {new Mplex67(), new Plaintext1()});
            Assert.Equals(3, connection.Protocols.Count);
        }

        [Test]
        public void CreatesOneStatsStream()
        {
            var a = new MemoryStream();
            var b = new MemoryStream();
            var connection = new PeerConnection();
            Assert.Equals(null, connection.Stream);

            connection.Stream = a;
            Assert.That(a, Is.Not.EqualTo(connection.Stream));

            connection.Stream = b;
            Assert.Equals(b, connection.Stream);
        }
    }
}
