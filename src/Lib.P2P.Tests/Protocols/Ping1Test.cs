﻿using System.Linq;
using System.Threading.Tasks;
using Lib.P2P.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests.Protocols
{
    [TestClass]
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

        [TestMethod]
        public async Task MultiAddress()
        {
            var swarmB = new SwarmService {LocalPeer = other};
            await swarmB.StartAsync();
            var pingB = new Ping1 {SwarmService = swarmB};
            await pingB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarm = new SwarmService {LocalPeer = self};
            await swarm.StartAsync();
            var pingA = new Ping1 {SwarmService = swarm};
            await pingA.StartAsync();
            try
            {
                await swarm.ConnectAsync(peerBAddress);
                var result = await pingA.PingAsync(other.Id, 4);
                Assert.IsTrue(result.All(r => r.Success));
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
                await pingB.StopAsync();
                await pingA.StopAsync();
            }
        }

        [TestMethod]
        public async Task PeerId()
        {
            var swarmB = new SwarmService {LocalPeer = other};
            await swarmB.StartAsync();
            var pingB = new Ping1 {SwarmService = swarmB};
            await pingB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarm = new SwarmService {LocalPeer = self};
            await swarm.StartAsync();
            var pingA = new Ping1 {SwarmService = swarm};
            await pingA.StartAsync();
            try
            {
                var result = await pingA.PingAsync(peerBAddress, 4);
                Assert.IsTrue(result.All(r => r.Success));
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
