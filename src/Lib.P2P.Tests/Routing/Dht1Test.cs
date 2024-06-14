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
using System.Threading.Tasks;
using Lib.P2P.Routing;
using MultiFormats;

namespace Lib.P2P.Tests.Routing
{
    public class Dht1Test
    {
        private Peer self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        private Peer other = new Peer
        {
            AgentVersion = "other",
            Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
            Addresses = new[]
            {
                new MultiAddress("/ip4/127.0.0.1/tcp/0")
            }
        };

        [Test]
        public async Task StoppedEventRaised()
        {
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            var stopped = false;
            dht.Stopped += (s, e) => { stopped = true; };
            await dht.StartAsync();
            await dht.StopAsync();
            Assert.That(stopped, Is.True);
        }

        [Test]
        public async Task SeedsRoutingTableFromSwarm()
        {
            var swarm = new SwarmService {LocalPeer = self};
            var peer = swarm.RegisterPeerAddress(
                "/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();
            try
            {
                Assert.That(dht.RoutingTable.Contains(peer), Is.True);
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task AddDiscoveredPeerToRoutingTable()
        {
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();
            try
            {
                var peer = swarm.RegisterPeerAddress(
                    "/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
                Assert.That(dht.RoutingTable.Contains(peer), Is.True);
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task RemovesPeerFromRoutingTable()
        {
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();
            try
            {
                var peer = swarm.RegisterPeerAddress(
                    "/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
                Assert.That(dht.RoutingTable.Contains(peer), Is.True);

                swarm.DeregisterPeer(peer);
                Assert.That(dht.RoutingTable.Contains(peer), Is.False);
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task ProcessFindNodeMessage_Self()
        {
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();
            try
            {
                var request = new DhtMessage
                {
                    Type = MessageType.FindNode,
                    Key = self.Id.ToArray()
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.That(response.CloserPeers.Length, Is.EqualTo(1));
                var ok = response.CloserPeers[0].TryToPeer(out var found);
                Assert.That(ok, Is.True);
                Assert.That(found, Is.EqualTo(self));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task ProcessFindNodeMessage_InRoutingTable()
        {
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();
            try
            {
                dht.RoutingTable.Add(other);
                var request = new DhtMessage
                {
                    Type = MessageType.FindNode,
                    Key = other.Id.ToArray()
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.That(response.CloserPeers.Length, Is.EqualTo(1));
                var ok = response.CloserPeers[0].TryToPeer(out var found);
                Assert.That(ok, Is.True);
                Assert.That(found, Is.EqualTo(other));
                Assert.That(other.Addresses.ToArray(),
                    Is.EquivalentTo(found.Addresses.Select(a => a.WithoutPeerId()).ToArray()));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task ProcessFindNodeMessage_InSwarm()
        {
            var swarmA = new SwarmService {LocalPeer = self};
            var swarmB = swarmA.RegisterPeerAddress(
                "/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
            var dht = new DhtService {SwarmService = swarmA};
            await dht.StartAsync();
            try
            {
                dht.RoutingTable.Add(swarmB);
                var request = new DhtMessage
                {
                    Type = MessageType.FindNode,
                    Key = swarmB.Id.ToArray()
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.That(response.CloserPeers.Length, Is.EqualTo(1));
                var ok = response.CloserPeers[0].TryToPeer(out var found);
                Assert.That(ok, Is.True);
                Assert.That(found, Is.EqualTo(swarmB));
                Assert.That(
                    swarmB.Addresses.Select(a => a.WithoutPeerId()).ToArray(),
                    Is.EquivalentTo(found.Addresses.Select(a => a.WithoutPeerId()).ToArray()));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task ProcessFindNodeMessage_Closest()
        {
            var swarm = new SwarmService {LocalPeer = self};
            swarm.RegisterPeerAddress("/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1a");
            swarm.RegisterPeerAddress("/ip4/127.0.0.2/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1b");
            swarm.RegisterPeerAddress("/ip4/127.0.0.3/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1c");
            swarm.RegisterPeerAddress("/ip4/127.0.0.4/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1d");
            swarm.RegisterPeerAddress("/ip4/127.0.0.5/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1e");
            var dht = new DhtService {SwarmService = swarm, CloserPeerCount = 3};
            await dht.StartAsync();
            try
            {
                dht.RoutingTable.Add(other);
                var request = new DhtMessage
                {
                    Type = MessageType.FindNode,
                    Key = other.Id.ToArray()
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.That(response.CloserPeers.Length, Is.EqualTo(3));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task ProcessFindNodeMessage_BadNodeId()
        {
            var swarm = new SwarmService {LocalPeer = self};
            swarm.RegisterPeerAddress("/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1a");
            swarm.RegisterPeerAddress("/ip4/127.0.0.2/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1b");
            swarm.RegisterPeerAddress("/ip4/127.0.0.3/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1c");
            swarm.RegisterPeerAddress("/ip4/127.0.0.4/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1d");
            swarm.RegisterPeerAddress("/ip4/127.0.0.5/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1e");
            var dht = new DhtService {SwarmService = swarm, CloserPeerCount = 3};
            await dht.StartAsync();
            try
            {
                dht.RoutingTable.Add(other);
                var request = new DhtMessage
                {
                    Type = MessageType.FindNode,
                    Key = new byte[] {0xFF, 1, 2, 3}
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.That(response.CloserPeers.Length, Is.EqualTo(3));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task ProcessFindNodeMessage_NoOtherPeers()
        {
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();
            try
            {
                var request = new DhtMessage
                {
                    Type = MessageType.FindNode,
                    Key = new MultiHash("QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h").ToArray()
                };
                var response = dht.ProcessFindNode(request, new DhtMessage());
                Assert.That(response.CloserPeers.Length, Is.EqualTo(0));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task ProcessGetProvidersMessage_HasCloserPeers()
        {
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();
            try
            {
                dht.RoutingTable.Add(other);
                Cid cid =
                    "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
                var request = new DhtMessage
                {
                    Type = MessageType.GetProviders,
                    Key = cid.Hash.ToArray()
                };
                var response = dht.ProcessGetProviders(request, new DhtMessage());
                Assert.That(0, Is.Not.EqualTo(response.CloserPeers.Length));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task ProcessGetProvidersMessage_HasProvider()
        {
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();
            try
            {
                swarm.RegisterPeer(other);
                Cid cid =
                    "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
                dht.ContentRouter.Add(cid, other.Id);
                var request = new DhtMessage
                {
                    Type = MessageType.GetProviders,
                    Key = cid.Hash.ToArray()
                };
                var response = dht.ProcessGetProviders(request, new DhtMessage());
                Assert.That(response.ProviderPeers.Length, Is.EqualTo(1));
                response.ProviderPeers[0].TryToPeer(out var found);
                Assert.That(found, Is.EqualTo(other));
                Assert.That(0, Is.Not.EqualTo(found.Addresses.Count()));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task ProcessAddProviderMessage()
        {
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();
            try
            {
                Cid cid =
                    "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
                var request = new DhtMessage
                {
                    Type = MessageType.AddProvider,
                    Key = cid.Hash.ToArray(),
                    ProviderPeers = new[]
                    {
                        new DhtPeerMessage
                        {
                            Id = other.Id.ToArray(),
                            Addresses = other.Addresses.Select(a => a.ToArray()).ToArray()
                        }
                    }
                };
                var response = dht.ProcessAddProvider(other, request, new DhtMessage());
                Assert.That(response, Is.Null);
                var providers = dht.ContentRouter.Get(cid).ToArray();
                Assert.That(providers.Length, Is.EqualTo(1));
                Assert.That(providers[0], Is.EqualTo(other.Id));

                var provider = swarm.KnownPeers.Single(p => p == other);
                Assert.That(0, Is.Not.EqualTo(provider.Addresses.Count()));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task QueryIsCancelled_WhenDhtStops()
        {
            var unknownPeer = new MultiHash("QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCxxx");
            var swarm = new SwarmService {LocalPeer = self};
            swarm.RegisterPeerAddress(
                "/ip4/178.62.158.247/tcp/4001/ipfs/QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd");
            swarm.RegisterPeerAddress(
                "/ip4/104.236.76.40/tcp/4001/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64");
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();
            await dht.FindPeerAsync(unknownPeer);
            await Task.Delay(400).ConfigureAwait(false);
            await dht.StopAsync();
        }

        [Test]
        public async Task FindPeer_NoPeers()
        {
            var unknownPeer = new MultiHash("QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCxxx");
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();

            try
            {
                var peer = await dht.FindPeerAsync(unknownPeer);
                Assert.That(peer, Is.Null);
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task FindPeer_Closest()
        {
            var unknownPeer = new MultiHash("QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCxxx");
            var swarm = new SwarmService {LocalPeer = self};
            await swarm.StartAsync();
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();
            dht.RoutingTable.Add(other);
            try
            {
                var peer = await dht.FindPeerAsync(unknownPeer);
                Assert.That(peer, Is.EqualTo(other));
            }
            finally
            {
                await swarm.StopAsync();
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task Add_FindProviders()
        {
            Cid cid = "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();

            try
            {
                dht.ContentRouter.Add(cid, other.Id);
                var peers = (await dht.FindProvidersAsync(cid, 1)).ToArray();
                Assert.That(peers.Length, Is.EqualTo(1));
                Assert.That(peers[0], Is.EqualTo(other));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [Test]
        public async Task Provide()
        {
            Cid cid = "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
            var swarm = new SwarmService {LocalPeer = self};
            var dht = new DhtService {SwarmService = swarm};
            await dht.StartAsync();

            try
            {
                await swarm.StartAsync();
                await swarm.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

                await dht.ProvideAsync(cid);
                var peers = (await dht.FindProvidersAsync(cid, 1)).ToArray();
                Assert.That(peers.Length, Is.EqualTo(1));
                Assert.That(peers[0], Is.EqualTo(self));
            }
            finally
            {
                await dht.StopAsync();
                await swarm.StopAsync();
            }
        }
    }
}
