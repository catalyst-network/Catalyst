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
            Assert.That(closeCount, Is.EqualTo(1));
        }

        [Test]
        public void Stats()
        {
            var stream = new MemoryStream();
            var connection = new PeerConnection {Stream = stream};
            Assert.That(connection.BytesRead, Is.EqualTo(0));
            Assert.That(connection.BytesWritten, Is.EqualTo(0));

            var buffer = new byte[] {1, 2, 3};
            connection.Stream.Write(buffer, 0, 3);
            Assert.That(connection.BytesRead, Is.EqualTo(0));
            Assert.That(connection.BytesWritten, Is.EqualTo(3));

            stream.Position = 0;
            connection.Stream.ReadByte();
            connection.Stream.ReadByte();
            Assert.That(connection.BytesRead, Is.EqualTo(2));
            Assert.That(connection.BytesWritten, Is.EqualTo(3));
        }

        [Test]
        public void Protocols()
        {
            var connection = new PeerConnection();
            Assert.That(connection.Protocols.Count, Is.EqualTo(0));

            connection.AddProtocol(new Identify1());
            Assert.That(connection.Protocols.Count, Is.EqualTo(1));

            connection.AddProtocols(new IPeerProtocol[] {new Mplex67(), new Plaintext1()});
            Assert.That(connection.Protocols.Count, Is.EqualTo(3));
        }

        [Test]
        public void CreatesOneStatsStream()
        {
            var a = new MemoryStream();
            var b = new MemoryStream();
            var connection = new PeerConnection();
            Assert.That(connection.Stream, Is.Null);

            connection.Stream = a;
            Assert.That(a, Is.EqualTo(connection.Stream));
        }
    }
}
