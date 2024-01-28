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
using MultiFormats;

namespace Lib.P2P.Tests
{
    public class ConnectionManagerTest
    {
        private MultiHash aId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
        private MultiHash bId = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h";

        [Test]
        public void IsConnected()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var connection = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.That(manager.IsConnected(peer), Is.False);
            manager.Add(connection);
            Assert.That(manager.IsConnected(peer), Is.True);
        }

        [Test]
        public void IsConnected_NotActive()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var connection = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.That(manager.IsConnected(peer), Is.False);

            manager.Add(connection);
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());

            connection.Stream = null;
            Assert.That(manager.IsConnected(peer), Is.False);
            Assert.Equals(0, manager.Connections.Count());
        }

        [Test]
        public void Add_Duplicate()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.Equals(a, manager.Add(a));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);

            Assert.Equals(b, manager.Add(b));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(2, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);
            Assert.That(b.Stream, Is.Not.Null);

            manager.Clear();
            Assert.Equals(0, manager.Connections.Count());
            Assert.That(a.Stream, Is.Null);
            Assert.That(b.Stream, Is.Null);
        }

        [Test]
        public void Add_Duplicate_SameConnection()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.Equals(a, manager.Add(a));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);

            Assert.Equals(a, manager.Add(a));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);
        }

        [Test]
        public void Add_Duplicate_PeerConnectedAddress()
        {
            var address = "/ip6/::1/tcp/4007";

            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId, ConnectedAddress = address};
            var a = new PeerConnection {RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null};

            Assert.Equals(a, manager.Add(a));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);
            Assert.Equals(address, peer.ConnectedAddress);

            Assert.Equals(b, manager.Add(b));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(2, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);
            Assert.That(b.Stream, Is.Not.Null);
            Assert.Equals(address, peer.ConnectedAddress);
        }

        [Test]
        public void Maintains_PeerConnectedAddress()
        {
            var address1 = "/ip4/127.0.0.1/tcp/4007";
            var address2 = "/ip4/127.0.0.2/tcp/4007";

            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, RemoteAddress = address1, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peer, RemoteAddress = address2, Stream = Stream.Null};

            Assert.Equals(a, manager.Add(a));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);
            Assert.Equals(address1, peer.ConnectedAddress);

            Assert.Equals(b, manager.Add(b));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(2, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);
            Assert.That(b.Stream, Is.Not.Null);
            Assert.Equals(address1, peer.ConnectedAddress);

            Assert.That(manager.Remove(a), Is.True);
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Null);
            Assert.That(b.Stream, Is.Not.Null);
            Assert.Equals(address2, peer.ConnectedAddress);

            Assert.That(manager.Remove(b), Is.True);
            Assert.That(manager.IsConnected(peer), Is.False);
            Assert.Equals(0, manager.Connections.Count());
            Assert.That(a.Stream, Is.Null);
            Assert.That(b.Stream, Is.Null);
            Assert.That(peer.ConnectedAddress, Is.Null);
        }

        [Test]
        public void Add_Duplicate_ExistingIsDead()
        {
            var address = "/ip6/::1/tcp/4007";

            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId, ConnectedAddress = address};
            var a = new PeerConnection {RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null};

            Assert.Equals(a, manager.Add(a));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);
            Assert.Equals(address, peer.ConnectedAddress);

            a.Stream = null;
            Assert.Equals(b, manager.Add(b));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Null);
            Assert.That(b.Stream, Is.Not.Null);
            Assert.Equals(address, peer.ConnectedAddress);
        }

        [Test]
        public void Add_NotActive()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.Equals(a, manager.Add(a));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);
            a.Stream = null;

            Assert.Equals(b, manager.Add(b));
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Null);
            Assert.That(b.Stream, Is.Not.Null);

            Assert.Equals(b, manager.Connections.First());
        }

        [Test]
        public void Remove_Connection()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            manager.Add(a);
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);

            Assert.That(manager.Remove(a), Is.True);
            Assert.That(manager.IsConnected(peer), Is.False);
            Assert.Equals(0, manager.Connections.Count());
            Assert.That(a.Stream, Is.Null);
        }

        [Test]
        public void Remove_PeerId()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            manager.Add(a);
            Assert.That(manager.IsConnected(peer), Is.True);
            Assert.Equals(1, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);

            Assert.That(manager.Remove(peer.Id), Is.True);
            Assert.That(manager.IsConnected(peer),Is.False);
            Assert.Equals(0, manager.Connections.Count());
            Assert.That(a.Stream, Is.Null);
        }

        [Test]
        public void Remove_DoesNotExist()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.That(manager.Remove(a), Is.False);
            Assert.That(manager.IsConnected(peer), Is.False);
            Assert.Equals(0, manager.Connections.Count());
            Assert.That(a.Stream, Is.Null);
        }

        [Test]
        public void Clear()
        {
            var manager = new ConnectionManager();
            var peerA = new Peer {Id = aId};
            var peerB = new Peer {Id = bId};
            var a = new PeerConnection {RemotePeer = peerA, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peerB, Stream = Stream.Null};

            Assert.Equals(a, manager.Add(a));
            Assert.Equals(b, manager.Add(b));
            Assert.That(manager.IsConnected(peerA), Is.True);
            Assert.That(manager.IsConnected(peerB), Is.True);
            Assert.Equals(2, manager.Connections.Count());
            Assert.That(a.Stream, Is.Not.Null);
            Assert.That(b.Stream, Is.Not.Null);

            manager.Clear();
            Assert.That(manager.IsConnected(peerA), Is.False);
            Assert.That(manager.IsConnected(peerB), Is.False);
            Assert.Equals(0, manager.Connections.Count());
            Assert.That(a.Stream, Is.Null);
            Assert.That(b.Stream, Is.Null);
        }

        [Test]
        public void PeerDisconnectedEvent_RemovingPeer()
        {
            var gotEvent = false;
            var manager = new ConnectionManager();
            manager.PeerDisconnected += (s, e) => gotEvent = true;
            var peerA = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peerA, Stream = Stream.Null};
            manager.Add(a);

            manager.Remove(peerA.Id);
            Assert.That(gotEvent, Is.True);
        }

        [Test]
        public void PeerDisconnectedEvent_RemovingConnection()
        {
            var gotEvent = 0;
            var manager = new ConnectionManager();
            manager.PeerDisconnected += (s, e) => gotEvent += 1;
            var peerA = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peerA, Stream = Stream.Null};
            manager.Add(a);

            manager.Remove(a);
            Assert.Equals(1, gotEvent);
        }

        [Test]
        public void PeerDisconnectedEvent_ConnectionClose()
        {
            var gotEvent = 0;
            var manager = new ConnectionManager();
            manager.PeerDisconnected += (s, e) => gotEvent += 1;
            var peerA = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peerA, Stream = Stream.Null};
            manager.Add(a);
            a.Dispose();
            Assert.Equals(1, gotEvent);
        }
    }
}
