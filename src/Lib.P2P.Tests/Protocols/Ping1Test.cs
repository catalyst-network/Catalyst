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
using Lib.P2P.Protocols;

namespace Lib.P2P.Tests.Protocols
{
    public class PingTest
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
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
        };

        [Test]
        public async Task MultiAddress()
        {
            var swarmB = new SwarmService {LocalPeer = other};
            await swarmB.StartAsync();
            var pingB = new Ping1(swarmB);
            await pingB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarm = new SwarmService {LocalPeer = self};
            await swarm.StartAsync();
            var pingA = new Ping1(swarm);
            await pingA.StartAsync();
            try
            {
                await swarm.ConnectAsync(peerBAddress);
                var result = await pingA.PingAsync(other.Id, 4);
                Assert.That(result.All(r => r.Success), Is.True);
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
                await pingB.StopAsync();
                await pingA.StopAsync();
            }
        }

        [Test]
        public async Task PeerId()
        {
            var swarmB = new SwarmService {LocalPeer = other};
            await swarmB.StartAsync();
            var pingB = new Ping1(swarmB);
            await pingB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarm = new SwarmService {LocalPeer = self};
            await swarm.StartAsync();
            var pingA = new Ping1(swarm);
            await pingA.StartAsync();
            try
            {
                var result = await pingA.PingAsync(peerBAddress, 4);
                Assert.That(result.All(r => r.Success), Is.True);
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
                await pingB.StopAsync();
                await pingA.StopAsync();
            }
        }
    }
}
