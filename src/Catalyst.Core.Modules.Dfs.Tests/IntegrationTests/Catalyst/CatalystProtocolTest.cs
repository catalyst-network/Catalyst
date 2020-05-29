//#region LICENSE

///**
//* Copyright (c) 2019 Catalyst Network
//*
//* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
//*
//* Catalyst.Node is free software: you can redistribute it and/or modify
//* it under the terms of the GNU General Public License as published by
//* the Free Software Foundation, either version 2 of the License, or
//* (at your option) any later version.
//*
//* Catalyst.Node is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//* GNU General Public License for more details.
//*
//* You should have received a copy of the GNU General Public License
//* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
//*/

//#endregion

//using System.Linq;
//using System.Threading.Tasks;
//using Catalyst.Protocol.Wire;
//using Lib.P2P.Protocols;
//using NUnit.Framework;
//using System.Reactive.Linq;
//using System;
//using System.Threading;
//using Catalyst.Protocol.IPPN;
//using Catalyst.Core.Lib.Extensions;

//namespace Lib.P2P.Tests.Protocols
//{
//    public class CatalystProtocolTest
//    {
//        private Peer self = new Peer
//        {
//            AgentVersion = "self",
//            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
//            PublicKey =
//                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
//        };

//        private Peer other = new Peer
//        {
//            AgentVersion = "other",
//            Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
//            PublicKey =
//                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
//        };

//        [Test]
//        public async Task Can_Send_ProtocolMessage_Using_MultiAddress()
//        {
//            var autoResetEvent = new AutoResetEvent(false);

//            var swarmB = new SwarmService(other);
//            await swarmB.StartAsync();
//            var catalystProtocolB = new CatalystProtocol(swarmB);
//            await catalystProtocolB.StartAsync();
//            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

//            var swarm = new SwarmService(self);
//            var catalystProtocolA = new CatalystProtocol(swarm);
//            await swarm.StartAsync();
//            var peerAAddress = await swarm.StartListeningAsync("/ip4/127.0.0.1/tcp/1");

//            await catalystProtocolA.StartAsync();
//            try
//            {
//                await swarm.ConnectAsync(peerBAddress);

//                var protocolMessage = new ProtocolMessage();
//                protocolMessage.PeerId = self.Addresses.First().ToString();
//                await catalystProtocolA.SendAsync(other.Id, protocolMessage);
//                catalystProtocolB.MessageStream.Subscribe(message =>
//                {
//                    autoResetEvent.Set();
//                });

//                autoResetEvent.WaitOne();
//            }
//            finally
//            {
//                await swarm.StopAsync();
//                await swarmB.StopAsync();
//                await catalystProtocolB.StopAsync();
//                await catalystProtocolA.StopAsync();
//            }
//        }

//        [Test]
//        public async Task Can_Send_ProtocolMessage_Using_PeerId()
//        {
//            var swarmB = new SwarmService(other);
//            await swarmB.StartAsync();
//            var pingB = new Ping1(swarmB);
//            await pingB.StartAsync();
//            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

//            var swarm = new SwarmService(self);
//            await swarm.StartAsync();
//            var pingA = new Ping1(swarm);
//            await pingA.StartAsync();
//            try
//            {
//                var result = await pingA.PingAsync(peerBAddress, 4);
//                Assert.IsTrue(result.All(r => r.Success));
//            }
//            finally
//            {
//                await swarm.StopAsync();
//                await swarmB.StopAsync();
//                await pingB.StopAsync();
//                await pingA.StopAsync();
//            }
//        }
//    }
//}
