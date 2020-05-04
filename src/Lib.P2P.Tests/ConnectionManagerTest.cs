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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiFormats;

namespace Lib.P2P.Tests
{
    [TestClass]
    public class ConnectionManagerTest
    {
        private MultiHash aId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
        private MultiHash bId = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h";

        [TestMethod]
        public void IsConnected()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var connection = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.IsFalse(manager.IsConnected(peer));
            manager.Add(connection);
            Assert.IsTrue(manager.IsConnected(peer));
        }

        [TestMethod]
        public void IsConnected_NotActive()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var connection = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.IsFalse(manager.IsConnected(peer));

            manager.Add(connection);
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());

            connection.Stream = null;
            Assert.IsFalse(manager.IsConnected(peer));
            Assert.AreEqual(0, manager.Connections.Count());
        }

        [TestMethod]
        public void Add_Duplicate()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);

            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(2, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.IsNotNull(b.Stream);

            manager.Clear();
            Assert.AreEqual(0, manager.Connections.Count());
            Assert.IsNull(a.Stream);
            Assert.IsNull(b.Stream);
        }

        [TestMethod]
        public void Add_Duplicate_SameConnection()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
        }

        [TestMethod]
        public void Add_Duplicate_PeerConnectedAddress()
        {
            var address = "/ip6/::1/tcp/4007";

            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId, ConnectedAddress = address};
            var a = new PeerConnection {RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null};

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.AreEqual(address, peer.ConnectedAddress);

            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(2, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.IsNotNull(b.Stream);
            Assert.AreEqual(address, peer.ConnectedAddress);
        }

        [TestMethod]
        public void Maintains_PeerConnectedAddress()
        {
            var address1 = "/ip4/127.0.0.1/tcp/4007";
            var address2 = "/ip4/127.0.0.2/tcp/4007";

            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, RemoteAddress = address1, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peer, RemoteAddress = address2, Stream = Stream.Null};

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.AreEqual(address1, peer.ConnectedAddress);

            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(2, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.IsNotNull(b.Stream);
            Assert.AreEqual(address1, peer.ConnectedAddress);

            Assert.IsTrue(manager.Remove(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNull(a.Stream);
            Assert.IsNotNull(b.Stream);
            Assert.AreEqual(address2, peer.ConnectedAddress);

            Assert.IsTrue(manager.Remove(b));
            Assert.IsFalse(manager.IsConnected(peer));
            Assert.AreEqual(0, manager.Connections.Count());
            Assert.IsNull(a.Stream);
            Assert.IsNull(b.Stream);
            Assert.IsNull(peer.ConnectedAddress);
        }

        [TestMethod]
        public void Add_Duplicate_ExistingIsDead()
        {
            var address = "/ip6/::1/tcp/4007";

            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId, ConnectedAddress = address};
            var a = new PeerConnection {RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null};

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.AreEqual(address, peer.ConnectedAddress);

            a.Stream = null;
            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNull(a.Stream);
            Assert.IsNotNull(b.Stream);
            Assert.AreEqual(address, peer.ConnectedAddress);
        }

        [TestMethod]
        public void Add_NotActive()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            a.Stream = null;

            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNull(a.Stream);
            Assert.IsNotNull(b.Stream);

            Assert.AreSame(b, manager.Connections.First());
        }

        [TestMethod]
        public void Remove_Connection()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            manager.Add(a);
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);

            Assert.IsTrue(manager.Remove(a));
            Assert.IsFalse(manager.IsConnected(peer));
            Assert.AreEqual(0, manager.Connections.Count());
            Assert.IsNull(a.Stream);
        }

        [TestMethod]
        public void Remove_PeerId()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            manager.Add(a);
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);

            Assert.IsTrue(manager.Remove(peer.Id));
            Assert.IsFalse(manager.IsConnected(peer));
            Assert.AreEqual(0, manager.Connections.Count());
            Assert.IsNull(a.Stream);
        }

        [TestMethod]
        public void Remove_DoesNotExist()
        {
            var manager = new ConnectionManager();
            var peer = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peer, Stream = Stream.Null};

            Assert.IsFalse(manager.Remove(a));
            Assert.IsFalse(manager.IsConnected(peer));
            Assert.AreEqual(0, manager.Connections.Count());
            Assert.IsNull(a.Stream);
        }

        [TestMethod]
        public void Clear()
        {
            var manager = new ConnectionManager();
            var peerA = new Peer {Id = aId};
            var peerB = new Peer {Id = bId};
            var a = new PeerConnection {RemotePeer = peerA, Stream = Stream.Null};
            var b = new PeerConnection {RemotePeer = peerB, Stream = Stream.Null};

            Assert.AreSame(a, manager.Add(a));
            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peerA));
            Assert.IsTrue(manager.IsConnected(peerB));
            Assert.AreEqual(2, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.IsNotNull(b.Stream);

            manager.Clear();
            Assert.IsFalse(manager.IsConnected(peerA));
            Assert.IsFalse(manager.IsConnected(peerB));
            Assert.AreEqual(0, manager.Connections.Count());
            Assert.IsNull(a.Stream);
            Assert.IsNull(b.Stream);
        }

        [TestMethod]
        public void PeerDisconnectedEvent_RemovingPeer()
        {
            var gotEvent = false;
            var manager = new ConnectionManager();
            manager.PeerDisconnected += (s, e) => gotEvent = true;
            var peerA = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peerA, Stream = Stream.Null};
            manager.Add(a);

            manager.Remove(peerA.Id);
            Assert.IsTrue(gotEvent);
        }

        [TestMethod]
        public void PeerDisconnectedEvent_RemovingConnection()
        {
            var gotEvent = 0;
            var manager = new ConnectionManager();
            manager.PeerDisconnected += (s, e) => gotEvent += 1;
            var peerA = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peerA, Stream = Stream.Null};
            manager.Add(a);

            manager.Remove(a);
            Assert.AreEqual(1, gotEvent);
        }

        [TestMethod]
        public void PeerDisconnectedEvent_ConnectionClose()
        {
            var gotEvent = 0;
            var manager = new ConnectionManager();
            manager.PeerDisconnected += (s, e) => gotEvent += 1;
            var peerA = new Peer {Id = aId};
            var a = new PeerConnection {RemotePeer = peerA, Stream = Stream.Null};
            manager.Add(a);
            a.Dispose();
            Assert.AreEqual(1, gotEvent);
        }
    }
}
