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

namespace Lib.P2P.Tests
{
    public sealed class AutoDialerTest
    {
        private Peer peerA = new Peer
        {
            AgentVersion = "A",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        private Peer peerB = new Peer
        {
            AgentVersion = "B",
            Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
        };

        private Peer peerC = new Peer
        {
            AgentVersion = "C",
            Id = "QmTcEBjSTSLjeu2oTiSoBSQQgqH5MADUsemXewn6rThoDT",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCAL8J1Lp6Ad5eYanOwNenXZ6Efvhk9wwFRXqqPn9UT+/JTxBvZPzQwK/FbPRczjZ/A1x8BSec1gvFCzcX4fkULAgMBAAE="
        };

        [Test]
        public void Defaults()
        {
            using (var dialer = new AutoDialer(new SwarmService()))
            {
                Assert.That(dialer.MinConnections, Is.EqualTo(AutoDialer.DefaultMinConnections));
            }
        }

        [Test]
        public async Task Connects_OnPeerDiscovered_When_Below_MinConnections()
        {
            var swarmA = new SwarmService {LocalPeer = peerA};
            await swarmA.StartAsync();
            await swarmA.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            try
            {
                using (new AutoDialer(swarmA))
                {
                    var other = swarmA.RegisterPeerAddress(peerBAddress);

                    // wait for the connection.
                    var endTime = DateTime.Now.AddSeconds(3);
                    while (other.ConnectedAddress == null)
                    {
                        if (DateTime.Now > endTime)
                            Assert.Fail("Did not do autodial");
                        await Task.Delay(100);
                    }
                }
            }
            finally
            {
                await swarmA?.StopAsync();
                await swarmB?.StopAsync();
            }
        }

        [Test]
        public async Task Noop_OnPeerDiscovered_When_NotBelow_MinConnections()
        {
            var swarmA = new SwarmService {LocalPeer = peerA};
            await swarmA.StartAsync();
            await swarmA.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            try
            {
                using (new AutoDialer(swarmA) {MinConnections = 0})
                {
                    var other = swarmA.RegisterPeerAddress(peerBAddress);

                    // wait for the connection.
                    var endTime = DateTime.Now.AddSeconds(3);
                    while (other.ConnectedAddress == null)
                    {
                        if (DateTime.Now > endTime)
                        {
                            return;
                        }
                        
                        await Task.Delay(100);
                    }

                    Assert.Fail("Autodial should not happen");
                }
            }
            finally
            {
                await swarmA.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public async Task Connects_OnPeerDisconnected_When_Below_MinConnections()
        {
            var swarmA = new SwarmService {LocalPeer = peerA};
            await swarmA.StartAsync();
            await swarmA.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarmC = new SwarmService {LocalPeer = peerC};
            await swarmC.StartAsync();
            var peerCAddress = await swarmC.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var isBConnected = false;
            swarmA.ConnectionEstablished += (s, conn) =>
            {
                if (conn.RemotePeer == peerB)
                {
                    isBConnected = true;
                }
            };

            try
            {
                using (new AutoDialer(swarmA) {MinConnections = 1})
                {
                    swarmA.RegisterPeerAddress(peerBAddress);
                    var c = swarmA.RegisterPeerAddress(peerCAddress);

                    // wait for the peer B connection.
                    var endTime = DateTime.Now.AddSeconds(3);
                    while (!isBConnected)
                    {
                        if (DateTime.Now > endTime)
                        {
                            Assert.Fail("Did not do autodial on peer discovered");
                        }
                        
                        await Task.Delay(100); // get cancellaton token
                    }

                    Assert.That(c.ConnectedAddress, Is.Null);
                    await swarmA.DisconnectAsync(peerBAddress);

                    // wait for the peer C connection.
                    endTime = DateTime.Now.AddSeconds(3);
                    while (c.ConnectedAddress == null)
                    {
                        if (DateTime.Now > endTime)
                            Assert.Fail("Did not do autodial on peer disconnected");
                        await Task.Delay(100);
                    }
                }
            }
            finally
            {
                await swarmA?.StopAsync();
                await swarmB?.StopAsync();
                await swarmC?.StopAsync();
            }
        }
    }
}
