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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.Protocols;
using MultiFormats;

namespace Lib.P2P.Tests.Protocols
{
    public class Identitfy1Test
    {
        [Test]
        public async Task RoundTrip()
        {
            var peerA = new Peer
            {
                Addresses = new MultiAddress[]
                {
                    "/ip4/127.0.0.1/tcp/4002/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"
                },
                AgentVersion = "agent/1",
                Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb",
                ProtocolVersion = "protocol/1",
                PublicKey =
                    "CAASpgIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCfBYU9c0n28u02N/XCJY8yIsRqRVO5Zw+6kDHCremt2flHT4AaWnwGLAG9YyQJbRTvWN9nW2LK7Pv3uoIlvUSTnZEP0SXB5oZeqtxUdi6tuvcyqTIfsUSanLQucYITq8Qw3IMBzk+KpWNm98g9A/Xy30MkUS8mrBIO9pHmIZa55fvclDkTvLxjnGWA2avaBfJvHgMSTu0D2CQcmJrvwyKMhLCSIbQewZd2V7vc6gtxbRovKlrIwDTmDBXbfjbLljOuzg2yBLyYxXlozO9blpttbnOpU4kTspUVJXglmjsv7YSIJS3UKt3544l/srHbqlwC5CgOgjlwNfYPadO8kmBfAgMBAAE="
            };
            var peerB = new Peer();
            var ms = new MemoryStream();
            var connection = new PeerConnection
            {
                LocalPeer = peerA,
                RemotePeer = peerB,
                Stream = ms
            };

            // Generate identify msg.
            var identify = new Identify1();
            await identify.ProcessMessageAsync(connection, ms);

            // Process identify msg.
            ms.Position = 0;
            await identify.UpdateRemotePeerAsync(peerB, ms, CancellationToken.None);

            Assert.Equals(peerA.AgentVersion, peerB.AgentVersion);
            Assert.Equals(peerA.Id, peerB.Id);
            Assert.Equals(peerA.ProtocolVersion, peerB.ProtocolVersion);
            Assert.Equals(peerA.PublicKey, peerB.PublicKey);
            Assert.That(peerA.Addresses.ToArray(), Is.EquivalentTo(peerB.Addresses.ToArray()));
        }

        [Test]
        public async Task InvalidPublicKey()
        {
            var peerA = new Peer
            {
                Addresses = new MultiAddress[]
                {
                    "/ip4/127.0.0.1/tcp/4002/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"
                },
                AgentVersion = "agent/1",
                Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb",
                ProtocolVersion = "protocol/1",
                PublicKey =
                    "BADSpgIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCfBYU9c0n28u02N/XCJY8yIsRqRVO5Zw+6kDHCremt2flHT4AaWnwGLAG9YyQJbRTvWN9nW2LK7Pv3uoIlvUSTnZEP0SXB5oZeqtxUdi6tuvcyqTIfsUSanLQucYITq8Qw3IMBzk+KpWNm98g9A/Xy30MkUS8mrBIO9pHmIZa55fvclDkTvLxjnGWA2avaBfJvHgMSTu0D2CQcmJrvwyKMhLCSIbQewZd2V7vc6gtxbRovKlrIwDTmDBXbfjbLljOuzg2yBLyYxXlozO9blpttbnOpU4kTspUVJXglmjsv7YSIJS3UKt3544l/srHbqlwC5CgOgjlwNfYPadO8kmBfAgMBAAE="
            };
            var peerB = new Peer
            {
                Id = peerA.Id
            };
            var ms = new MemoryStream();
            var connection = new PeerConnection
            {
                LocalPeer = peerA,
                RemotePeer = peerB,
                Stream = ms
            };

            // Generate identify msg.
            var identify = new Identify1();
            await identify.ProcessMessageAsync(connection, ms);

            // Process identify msg.
            ms.Position = 0;
            ExceptionAssert.Throws<InvalidDataException>(() =>
            {
                identify.UpdateRemotePeerAsync(peerB, ms, CancellationToken.None).Wait();
            });
        }

        [Test]
        public async Task MustHavePublicKey()
        {
            var peerA = new Peer
            {
                Addresses = new MultiAddress[]
                {
                    "/ip4/127.0.0.1/tcp/4002/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"
                },
                AgentVersion = "agent/1",
                Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb",
                ProtocolVersion = "protocol/1",
                PublicKey = ""
            };
            var peerB = new Peer
            {
                Id = peerA.Id
            };
            var ms = new MemoryStream();
            var connection = new PeerConnection
            {
                LocalPeer = peerA,
                RemotePeer = peerB,
                Stream = ms
            };

            // Generate identify msg.
            var identify = new Identify1();
            await identify.ProcessMessageAsync(connection, ms);

            // Process identify msg.
            ms.Position = 0;
            ExceptionAssert.Throws<InvalidDataException>(() =>
            {
                identify.UpdateRemotePeerAsync(peerB, ms, CancellationToken.None).Wait();
            });
        }
    }
}
