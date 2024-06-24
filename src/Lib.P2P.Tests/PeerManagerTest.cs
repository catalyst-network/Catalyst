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
using System.Threading.Tasks;
using MultiFormats;

namespace Lib.P2P.Tests
{
    public class PeerManagerTest
    {
        private Peer self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        [Test]
        public void IsNotReachable()
        {
            var peer = new Peer {Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"};
            var manager = new PeerManager {SwarmService = new SwarmService()};
            Assert.That(manager.DeadPeers.Count, Is.EqualTo(0));

            manager.SetNotReachable(peer);
            Assert.That(manager.DeadPeers.ContainsKey(peer), Is.True);
            Assert.That(manager.DeadPeers.Count, Is.EqualTo(1));

            manager.SetNotReachable(peer);
            Assert.That(manager.DeadPeers.ContainsKey(peer), Is.True);
            Assert.That(manager.DeadPeers.Count, Is.EqualTo(1));

            manager.SetReachable(peer);
            Assert.That(manager.DeadPeers.ContainsKey(peer), Is.False);
            Assert.That(manager.DeadPeers.Count, Is.EqualTo(0));
        }

        [Test]
        public void BlackListsThePeer()
        {
            var peer = new Peer {Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"};
            var manager = new PeerManager {SwarmService = new SwarmService()};
            Assert.That(manager.DeadPeers.Count, Is.EqualTo(0));

            manager.SetNotReachable(peer);
            Assert.That(
                manager.SwarmService.IsAllowed((MultiAddress) "/p2p/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"), Is.False);

            manager.SetReachable(peer);
            Assert.That(
                manager.SwarmService.IsAllowed((MultiAddress) "/p2p/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"), Is.True);
        }

        [Test]
        public async Task Backoff_Increases()
        {
            var peer = new Peer
            {
                Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTxx",
                Addresses = new MultiAddress[]
                {
                    "/ip4/127.0.0.1/tcp/4040/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTxx"
                }
            };
            var swarm = new SwarmService {LocalPeer = self};
            var manager = new PeerManager
            {
                SwarmService = swarm,
                InitialBackoff = TimeSpan.FromMilliseconds(100),
            };
            Assert.That(manager.DeadPeers.Count, Is.EqualTo(0));

            try
            {
                await swarm.StartAsync();
                await manager.StartAsync();
                try
                {
                    await swarm.ConnectAsync(peer);
                }
                catch
                {
                    // ignored
                }

                Assert.That(manager.DeadPeers.Count, Is.EqualTo(1));

                var end = DateTime.Now + TimeSpan.FromSeconds(4);
                while (DateTime.Now <= end)
                    if (manager.DeadPeers[peer].Backoff > manager.InitialBackoff)
                        return;
                Assert.Fail("backoff did not increase");
            }
            finally
            {
                await swarm.StopAsync();
                await manager.StopAsync();
            }
        }

        [Test]
        public async Task PermanentlyDead()
        {
            var peer = new Peer
            {
                Id = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb",
                Addresses = new MultiAddress[]
                {
                    "/ip4/127.0.0.1/tcp/4040/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"
                }
            };
            var swarm = new SwarmService {LocalPeer = self};
            var manager = new PeerManager
            {
                SwarmService = swarm,
                InitialBackoff = TimeSpan.FromMilliseconds(100),
                MaxBackoff = TimeSpan.FromMilliseconds(200),
            };
            Assert.That(manager.DeadPeers.Count, Is.EqualTo(0));

            try
            {
                await swarm.StartAsync();
                await manager.StartAsync();
                try
                {
                    await swarm.ConnectAsync(peer);
                }
                catch
                {
                    // ignored
                }

                Assert.That(manager.DeadPeers.Count, Is.EqualTo(1));

                var end = DateTime.Now + TimeSpan.FromSeconds(6);
                while (DateTime.Now <= end)
                    if (manager.DeadPeers[peer].NextAttempt == DateTime.MaxValue)
                        return;
                Assert.Fail("not truely dead");
            }
            finally
            {
                await swarm.StopAsync();
                await manager.StopAsync();
            }
        }
    }
}
