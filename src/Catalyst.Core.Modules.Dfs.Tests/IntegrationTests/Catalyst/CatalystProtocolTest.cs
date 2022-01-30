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

using System.Linq;
using System.Threading.Tasks;
using Catalyst.Protocol.Wire;
using Lib.P2P.Protocols;
using NUnit.Framework;
using System.Reactive.Linq;
using System;
using System.Threading;
using Catalyst.TestUtils;

namespace Lib.P2P.Tests.Protocols
{
    [TestFixture]
    [Category(Traits.IntegrationTest)]
    public class CatalystProtocolTest
    {
        private readonly Peer self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        private readonly Peer other = new Peer
        {
            AgentVersion = "other",
            Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
        };

        [Test]
        public async Task Can_Send_ProtocolMessage_Using_PeerId()
        {
            var autoResetEvent = new AutoResetEvent(false);

            var swarmB = new SwarmService(other);
            await swarmB.StartAsync();
            var catalystProtocolB = new CatalystProtocol(swarmB);
            await catalystProtocolB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/5001");

            var swarm = new SwarmService(self);
            var catalystProtocolA = new CatalystProtocol(swarm);
            await swarm.StartAsync();
            await swarm.StartListeningAsync("/ip4/127.0.0.1/tcp/5002");

            await catalystProtocolA.StartAsync();
            try
            {
                await swarm.ConnectAsync(peerBAddress);

                var protocolMessage = new ProtocolMessage
                {
                    Address = self.Addresses.First().ToString()
                };

                await catalystProtocolA.SendAsync(other.Id, protocolMessage);
                catalystProtocolB.MessageStream.Subscribe(message =>
                {
                    autoResetEvent.Set();
                });

                autoResetEvent.WaitOne();
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
                await catalystProtocolB.StopAsync();
                await catalystProtocolA.StopAsync();
            }
        }

        [Test]
        public async Task Can_Send_ProtocolMessage_Using_MultiAddress()
        {
            var autoResetEvent = new AutoResetEvent(false);

            var swarmB = new SwarmService(other);
            await swarmB.StartAsync();
            var catalystProtocolB = new CatalystProtocol(swarmB);
            await catalystProtocolB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/5003");

            var swarm = new SwarmService(self);
            var catalystProtocolA = new CatalystProtocol(swarm);
            await swarm.StartAsync();
            await swarm.StartListeningAsync("/ip4/127.0.0.1/tcp/5004");

            await catalystProtocolA.StartAsync();
            try
            {
                await swarm.ConnectAsync(peerBAddress);

                var protocolMessage = new ProtocolMessage
                {
                    Address = self.Addresses.First().ToString()
                };

                await catalystProtocolA.SendAsync(peerBAddress, protocolMessage);
                catalystProtocolB.MessageStream.Subscribe(message =>
                {
                    autoResetEvent.Set();
                });

                autoResetEvent.WaitOne();
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
                await catalystProtocolB.StopAsync();
                await catalystProtocolA.StopAsync();
            }
        }
    }
}
