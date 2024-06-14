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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MultiFormats;

namespace Lib.P2P.Tests
{
    public sealed class SwarmTest
    {
        private readonly MultiAddress _mars =
            "/ip4/10.1.10.10/tcp/29087/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";

        private readonly MultiAddress _venus =
            "/ip4/104.236.76.40/tcp/4001/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64";

        private readonly MultiAddress _earth =
            "/ip4/178.62.158.247/tcp/4001/ipfs/QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd";

        private readonly Peer _self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        private readonly Peer _other = new Peer
        {
            AgentVersion = "other",
            Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
        };

        [Test]
        public async Task Start_Stop()
        {
            var swarm = new SwarmService
            {
                LocalPeer = _self
            };
            await swarm.StartAsync();
            await swarm.StopAsync();
        }

        [Test]
        public void Start_NoLocalPeer()
        {
            var swarm = new SwarmService();
            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                swarm.StartAsync().Wait();
            });
        }

        [Test]
        public void NewPeerAddress()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            swarm.RegisterPeerAddress(_mars);
            Assert.That(swarm.KnownPeerAddresses.Contains(_mars), Is.True);
        }

        [Test]
        public void NewPeerAddress_Self()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            var selfAddress = "/ip4/178.62.158.247/tcp/4001/ipfs/" + _self.Id;
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = swarm.RegisterPeerAddress(selfAddress);
            });

            selfAddress = "/ip4/178.62.158.247/tcp/4001/p2p/" + _self.Id;
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = swarm.RegisterPeerAddress(selfAddress);
            });
        }

        //turn off blacklisting
        //[Test]
        //public void NewPeerAddress_BlackList()
        //{
        //    var swarm = new SwarmService {LocalPeer = _self};
        //    swarm.BlackList.Add(_mars);

        //    ExceptionAssert.Throws<Exception>(() =>
        //    {
        //        var _ = swarm.RegisterPeerAddress(_mars);
        //    });
        //    Assert.IsFalse(swarm.KnownPeerAddresses.Contains(_mars));

        //    Assert.IsNotNull(swarm.RegisterPeerAddress(_venus));
        //    Assert.IsTrue(swarm.KnownPeerAddresses.Contains(_venus));
        //}

        //turn off blacklisting
        //[Test]
        //public void NewPeerAddress_WhiteList()
        //{
        //    var swarm = new SwarmService {LocalPeer = _self};
        //    swarm.WhiteList.Add(_venus);

        //    ExceptionAssert.Throws<Exception>(() =>
        //    {
        //        var _ = swarm.RegisterPeerAddress(_mars);
        //    });
        //    Assert.IsFalse(swarm.KnownPeerAddresses.Contains(_mars));

        //    Assert.IsNotNull(swarm.RegisterPeerAddress(_venus));
        //    Assert.IsTrue(swarm.KnownPeerAddresses.Contains(_venus));
        //}

        [Test]
        public void NewPeerAddress_InvalidAddress_MissingPeerId()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = swarm.RegisterPeerAddress("/ip4/10.1.10.10/tcp/29087");
            });
            Assert.That(swarm.KnownPeerAddresses.Count(), Is.EqualTo(0));
        }

        [Test]
        public void NewPeerAddress_Duplicate()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            swarm.RegisterPeerAddress(_mars);
            Assert.That(swarm.KnownPeerAddresses.Count(), Is.EqualTo(1));

            swarm.RegisterPeerAddress(_mars);
            Assert.That(swarm.KnownPeerAddresses.Count(), Is.EqualTo(1));
        }

        [Test]
        public void KnownPeers()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            Assert.That(swarm.KnownPeers.Count(), Is.EqualTo(0));
            Assert.That(swarm.KnownPeerAddresses.Count(), Is.EqualTo(0));

            swarm.RegisterPeerAddress("/ip4/10.1.10.10/tcp/29087/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3");
            Assert.That(swarm.KnownPeers.Count(), Is.EqualTo(1));
            Assert.That(swarm.KnownPeerAddresses.Count(), Is.EqualTo(1));

            swarm.RegisterPeerAddress("/ip4/10.1.10.11/tcp/29087/p2p/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3");
            Assert.That(swarm.KnownPeers.Count(), Is.EqualTo(1));
            Assert.That(swarm.KnownPeerAddresses.Count(), Is.EqualTo(2));

            swarm.RegisterPeerAddress(_venus);
            Assert.That(swarm.KnownPeers.Count(), Is.EqualTo(2));
            Assert.That(swarm.KnownPeerAddresses.Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task Connect_Disconnect()
        {
            var peerB = new Peer
            {
                AgentVersion = "peerB",
                Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                PublicKey =
                    "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
            Assert.That(peerB.Addresses.Any(), Is.True);

            var swarm = new SwarmService {LocalPeer = _self};
            await swarm.StartAsync();
            try
            {
                var remotePeer = (await swarm.ConnectAsync(peerBAddress)).RemotePeer;
                Assert.That(remotePeer.ConnectedAddress, Is.Not.Null);
                Assert.That(peerB.PublicKey, Is.EqualTo(remotePeer.PublicKey));
                Assert.That(remotePeer.IsValid(), Is.True);
                Assert.That(swarm.KnownPeers.Contains(peerB), Is.True);

                // wait for swarmB to settle
                var endTime = DateTime.Now.AddSeconds(3);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        Assert.Fail("swarmB does not know about self");
                    }
                    
                    if (swarmB.KnownPeers.Contains(_self))
                    {
                        break;
                    }
                    
                    await Task.Delay(100);
                }

                var me = swarmB.KnownPeers.First(p => p == _self);
                Assert.That(_self.Id, Is.EqualTo(me.Id));
                Assert.That(_self.PublicKey, Is.EqualTo(me.PublicKey));
                Assert.That(me.ConnectedAddress, Is.Not.Null);

                // Check disconnect
                await swarm.DisconnectAsync(peerBAddress);
                Assert.That(remotePeer.ConnectedAddress, Is.Null);
                Assert.That(swarm.KnownPeers.Contains(peerB), Is.True);
                Assert.That(swarmB.KnownPeers.Contains(_self), Is.True);

                // wait for swarmB to settle
                endTime = DateTime.Now.AddSeconds(3);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        Assert.Fail("swarmB did not close connection.");
                    }

                    if (me.ConnectedAddress == null)
                    {
                        break;
                    }
                    
                    await Task.Delay(100);
                }
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public async Task Connect_Disconnect_Reconnect()
        {
            var peerB = new Peer
            {
                AgentVersion = "peerB",
                Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                PublicKey =
                    "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
            Assert.That(peerB.Addresses.Any(), Is.True);

            var swarm = new SwarmService {LocalPeer = _self};
            await swarm.StartAsync();
            await swarm.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
            try
            {
                var remotePeer = (await swarm.ConnectAsync(peerBAddress)).RemotePeer;
                Assert.That(remotePeer.ConnectedAddress, Is.Not.Null);
                Assert.That(peerB.PublicKey, Is.EqualTo(remotePeer.PublicKey));
                Assert.That(remotePeer.IsValid(), Is.True);
                Assert.That(swarm.KnownPeers.Contains(peerB), Is.True);

                // wait for swarmB to settle
                var endTime = DateTime.Now.AddSeconds(3);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        Assert.Fail("swarmB does not know about self");
                    }
                    
                    if (swarmB.KnownPeers.Contains(_self))
                    {
                        break;
                    }
                    
                    await Task.Delay(100);
                }

                var me = swarmB.KnownPeers.First(p => p == _self);
                Assert.That(_self.Id, Is.EqualTo(me.Id));
                Assert.That(_self.PublicKey, Is.EqualTo(me.PublicKey));
                Assert.That(me.ConnectedAddress, Is.Not.Null);

                // Check disconnect
                await swarm.DisconnectAsync(peerBAddress);
                Assert.That(remotePeer.ConnectedAddress, Is.Null);
                Assert.That(swarm.KnownPeers.Contains(peerB), Is.True);
                Assert.That(swarmB.KnownPeers.Contains(_self), Is.True);

                // wait for swarmB to settle
                endTime = DateTime.Now.AddSeconds(3);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        Assert.Fail("swarmB did not close connection.");
                    }
                    
                    if (me.ConnectedAddress == null)
                    {
                        break;
                    }
                    
                    await Task.Delay(100);
                }

                // Reconnect
                remotePeer = (await swarm.ConnectAsync(peerBAddress)).RemotePeer;
                Assert.That(remotePeer.ConnectedAddress, Is.Not.Null);
                Assert.That(peerB.PublicKey, Is.EqualTo(remotePeer.PublicKey));
                Assert.That(remotePeer.IsValid(), Is.True);
                Assert.That(swarm.KnownPeers.Contains(peerB), Is.True);
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public async Task RemotePeer_Contains_ConnectedAddress1()
        {
            var peerB = new Peer
            {
                AgentVersion = "peerB",
                Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                PublicKey =
                    "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/0.0.0.0/tcp/0");

            var swarm = new SwarmService {LocalPeer = _self};
            await swarm.StartAsync();
            try
            {
                var connection = await swarm.ConnectAsync(peerBAddress);
                var remote = connection.RemotePeer;
                Assert.That(remote.ConnectedAddress, Is.EqualTo(peerBAddress));
                Assert.That(remote.Addresses.ToArray(), Contains.Item(peerBAddress));
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public async Task RemotePeer_Contains_ConnectedAddress2()
        {
            // Only works on Windows because connecting to 127.0.0.100 is allowed
            // when listening on 0.0.0.0
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            var peerB = new Peer
            {
                AgentVersion = "peerB",
                Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                PublicKey =
                    "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/0.0.0.0/tcp/0");
            var peerBPort = peerBAddress.Protocols[1].Value;
            Assert.That(peerB.Addresses.Any(), Is.True);

            var swarm = new SwarmService {LocalPeer = _self};
            await swarm.StartAsync();
            try
            {
                MultiAddress ma = $"/ip4/127.0.0.100/tcp/{peerBPort}/ipfs/{peerB.Id}";
                var connection = await swarm.ConnectAsync(ma);
                var remote = connection.RemotePeer;
                Assert.That(remote.ConnectedAddress, Is.EqualTo(ma));
                Assert.That(remote.Addresses.ToArray(), Contains.Item(ma));
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public async Task Connect_CancelsOnStop()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            var venus = new Peer
            {
                Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ",
                Addresses = new MultiAddress[]
                {
                    "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ", // mars.i.ipfs.io
                }
            };

            await swarm.StartAsync();
            var a = swarm.ConnectAsync(venus);
            Assert.That(a.IsCanceled || a.IsFaulted, Is.False);

            await swarm.StopAsync();
            var endTime = DateTime.Now.AddSeconds(3);
            while (!a.IsCanceled && !a.IsFaulted)
            {
                if (DateTime.Now > endTime)
                {
                    Assert.Fail("swarm did not cancel pending connection.");
                }
                
                await Task.Delay(100);
            }

            Assert.That(a.IsCanceled || a.IsFaulted, Is.True);
        }

        [Test]
        public async Task Connect_IsPending()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            var venus = new Peer
            {
                Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ",
                Addresses = new MultiAddress[]
                {
                    "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ", // mars.i.ipfs.io
                }
            };

            await swarm.StartAsync();
            try
            {
                Assert.That(swarm.HasPendingConnection(venus), Is.False);

                var _ = swarm.ConnectAsync(venus);
                Assert.That(swarm.HasPendingConnection(venus), Is.True);
            }
            finally
            {
                await swarm.StopAsync();
            }
        }

        [Test]
        public async Task Connect_WithSomeUnreachableAddresses()
        {
            const string bid = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h";
            var peerB = new Peer
            {
                AgentVersion = "peerB",
                Id = bid,
                PublicKey =
                    "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE=",
                Addresses = new MultiAddress[]
                {
                    $"/ip4/127.0.0.2/tcp/2/ipfs/{bid}",
                    $"/ip4/127.0.0.3/tcp/3/ipfs/{bid}"
                }
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmB.StartAsync();
            var _ = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
            Assert.That(peerB.Addresses.Any(), Is.True);

            var swarm = new SwarmService {LocalPeer = _self};
            await swarm.StartAsync();
            try
            {
                var remotePeer = (await swarm.ConnectAsync(peerB)).RemotePeer;
                Assert.That(remotePeer.ConnectedAddress, Is.Not.Null);
                Assert.That(peerB.PublicKey, Is.EqualTo(remotePeer.PublicKey));
                Assert.That(remotePeer.IsValid(), Is.True);
                Assert.That(swarm.KnownPeers.Contains(peerB), Is.True);
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public async Task ConnectionEstablished()
        {
            var peerB = new Peer
            {
                AgentVersion = "peerB",
                Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                PublicKey =
                    "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            var swarmBConnections = 0;
            swarmB.ConnectionEstablished += (s, e) => { ++swarmBConnections; };
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarm = new SwarmService {LocalPeer = _self};
            var swarmConnections = 0;
            swarm.ConnectionEstablished += (s, e) => { ++swarmConnections; };
            await swarm.StartAsync();
            try
            {
                var _ = await swarm.ConnectAsync(peerBAddress);
                Assert.That(1, Is.EqualTo(swarmConnections));

                // wait for swarmB to settle
                var endTime = DateTime.Now.AddSeconds(3);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        Assert.Fail("swarmB did not raise event.");
                    }

                    if (swarmBConnections == 1)
                    {
                        break;
                    }
                    
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public void Connect_No_Transport()
        {
            const string remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            MultiAddress remoteAddress = $"/ip4/127.0.0.1/ipfs/{remoteId}";
            var swarm = new SwarmService {LocalPeer = _self};
            swarm.StartAsync().Wait();
            try
            {
                ExceptionAssert.Throws<Exception>(() =>
                {
                    var _ = swarm.ConnectAsync(remoteAddress).Result;
                });
            }
            finally
            {
                swarm.StopAsync().Wait();
            }
        }

        [Test]
        public void Connect_Refused()
        {
            const string remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            MultiAddress remoteAddress = $"/ip4/127.0.0.1/tcp/4040/ipfs/{remoteId}";
            var swarm = new SwarmService {LocalPeer = _self};
            swarm.StartAsync().Wait();
            try
            {
                ExceptionAssert.Throws<Exception>(() =>
                {
                    var _ = swarm.ConnectAsync(remoteAddress).Result;
                });
            }
            finally
            {
                swarm.StopAsync().Wait();
            }
        }

        [Test]
        public void Connect_Failure_Event()
        {
            const string remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            MultiAddress remoteAddress = $"/ip4/127.0.0.1/tcp/4040/ipfs/{remoteId}";
            var swarm = new SwarmService {LocalPeer = _self};
            Peer unreachable = null;
            swarm.PeerNotReachable += (s, e) => { unreachable = e; };
            swarm.StartAsync().Wait();
            try
            {
                ExceptionAssert.Throws<Exception>(() =>
                {
                    var _ = swarm.ConnectAsync(remoteAddress).Result;
                });
            }
            finally
            {
                swarm.StopAsync().Wait();
            }

            Assert.That(unreachable, Is.Not.Null);
            Assert.That(remoteId, Is.EqualTo(unreachable.Id.ToBase58()));
        }

        [Test]
        public void Connect_Not_Peer()
        {
            const string remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            MultiAddress remoteAddress = $"/dns/npmjs.com/tcp/80/ipfs/{remoteId}";
            var swarm = new SwarmService {LocalPeer = _self};
            swarm.StartAsync().Wait();
            try
            {
                ExceptionAssert.Throws<Exception>(() =>
                {
                    var _ = swarm.ConnectAsync(remoteAddress).Result;
                });
            }
            finally
            {
                swarm.StopAsync().Wait();
            }
        }

        [Test]
        public void Connect_Cancelled()
        {
            var cs = new CancellationTokenSource();
            cs.Cancel();
            const string remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            MultiAddress remoteAddress = $"/ip4/127.0.0.1/tcp/4002/ipfs/{remoteId}";
            var swarm = new SwarmService {LocalPeer = _self};
            swarm.StartAsync().Wait(cs.Token);
            try
            {
                ExceptionAssert.Throws<Exception>(() =>
                {
                    var _ = swarm.ConnectAsync(remoteAddress, cs.Token).Result;
                });
            }
            finally
            {
                swarm.StopAsync().Wait(cs.Token);
            }
        }

        [Test]
        public void Connecting_To_Blacklisted_Address()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            swarm.BlackList.Add(_mars);
            swarm.StartAsync().Wait();
            try
            {
                ExceptionAssert.Throws<Exception>(() =>
                {
                    var _ = swarm.ConnectAsync(_mars).Result;
                });
            }
            finally
            {
                swarm.StopAsync().Wait();
            }
        }

        [Test]
        public void Connecting_To_Self()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            swarm.StartAsync().Wait();
            try
            {
                ExceptionAssert.Throws<Exception>(() =>
                {
                    var _ = swarm.ConnectAsync(_earth).Result;
                });
            }
            finally
            {
                swarm.StopAsync().Wait();
            }
        }

        [Test]
        public async Task Connecting_To_Self_Indirect()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            await swarm.StartAsync();
            try
            {
                var listen = await swarm.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
                var bad = listen.Clone();
                bad.Protocols[2].Value = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
                ExceptionAssert.Throws<Exception>(() => { swarm.ConnectAsync(bad).Wait(); });
            }
            finally
            {
                await swarm.StopAsync();
            }
        }

        [Test]
        public async Task PeerDisconnected()
        {
            var peerB = new Peer
            {
                AgentVersion = "peerB",
                Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                PublicKey =
                    "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarm = new SwarmService {LocalPeer = _self};
            var swarmConnections = 0;
            swarm.ConnectionEstablished += (s, e) => { ++swarmConnections; };
            swarm.PeerDisconnected += (s, e) => { --swarmConnections; };
            await swarm.StartAsync();
            try
            {
                var _ = await swarm.ConnectAsync(peerBAddress);
                Assert.That(1, Is.EqualTo(swarmConnections));

                await swarm.StopAsync();
                Assert.That(0, Is.EqualTo(swarmConnections));
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public async Task Listening()
        {
            var peerA = new Peer
            {
                Id = _self.Id,
                PublicKey = _self.PublicKey,
                AgentVersion = _self.AgentVersion
            };
            MultiAddress addr = "/ip4/127.0.0.1/tcp/0";
            var swarmA = new SwarmService {LocalPeer = peerA};
            var peerB = new Peer
            {
                Id = _other.Id,
                PublicKey = _other.PublicKey,
                AgentVersion = _other.AgentVersion
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmA.StartAsync();
            await swarmB.StartAsync();
            try
            {
                var another = await swarmA.StartListeningAsync(addr);
                Assert.That(peerA.Addresses.Contains(another), Is.True);

                await swarmB.ConnectAsync(another);
                Assert.That(swarmB.KnownPeers.Contains(peerA), Is.True);

                await swarmA.StopListeningAsync(addr);
                Assert.That(0, Is.EqualTo(peerA.Addresses.Count()));
            }
            finally
            {
                await swarmA.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public async Task Listening_Start_Stop()
        {
            var peer = new Peer
            {
                Id = _self.Id,
                PublicKey = _self.PublicKey,
                AgentVersion = _self.AgentVersion
            };
            MultiAddress addr = "/ip4/0.0.0.0/tcp/0";
            var swarm = new SwarmService {LocalPeer = peer};
            await swarm.StartAsync();

            try
            {
                await swarm.StartListeningAsync(addr);
                Assert.That(peer.Addresses.Any(), Is.True);

                await swarm.StopListeningAsync(addr);
                Assert.That(0, Is.EqualTo(peer.Addresses.Count()));

                await swarm.StartListeningAsync(addr);
                Assert.That(peer.Addresses.Any(), Is.True);

                await swarm.StopListeningAsync(addr);
                Assert.That(0, Is.EqualTo(peer.Addresses.Count()));
            }
            finally
            {
                await swarm.StopAsync();
            }
        }

        [Test]
        public async Task Stop_Closes_Listeners()
        {
            var peer = new Peer
            {
                Id = _self.Id,
                PublicKey = _self.PublicKey,
                AgentVersion = _self.AgentVersion
            };
            MultiAddress addr = "/ip4/0.0.0.0/tcp/0";
            var swarm = new SwarmService {LocalPeer = peer};

            try
            {
                await swarm.StartAsync();
                await swarm.StartListeningAsync(addr);
                Assert.That(peer.Addresses.Any(), Is.True);
                await swarm.StopAsync();
                Assert.That(0, Is.EqualTo(peer.Addresses.Count()));

                await swarm.StartAsync();
                await swarm.StartListeningAsync(addr);
                Assert.That(peer.Addresses.Any(), Is.True);
                await swarm.StopAsync();
                Assert.That(0, Is.EqualTo(peer.Addresses.Count()));
            }
            catch (Exception)
            {
                await swarm.StopAsync();
                throw;
            }
        }

        [Test]
        public async Task Listening_Event()
        {
            var peer = new Peer
            {
                Id = _self.Id,
                PublicKey = _self.PublicKey,
                AgentVersion = _self.AgentVersion
            };
            MultiAddress addr = "/ip4/127.0.0.1/tcp/0";
            var swarm = new SwarmService {LocalPeer = peer};
            Peer listeningPeer = null;
            swarm.ListenerEstablished += (s, e) => { listeningPeer = e; };
            try
            {
                await swarm.StartListeningAsync(addr);
                Assert.That(peer, Is.EqualTo(listeningPeer));
                Assert.That(0, Is.Not.EqualTo(peer.Addresses.Count()));
            }
            finally
            {
                await swarm.StopAsync();
            }
        }

        [Test]
        public async Task Listening_AnyPort()
        {
            var peerA = new Peer
            {
                Id = _self.Id,
                PublicKey = _self.PublicKey,
                AgentVersion = _self.AgentVersion
            };
            MultiAddress addr = "/ip4/127.0.0.1/tcp/0";
            var swarmA = new SwarmService {LocalPeer = peerA};
            var peerB = new Peer
            {
                Id = _other.Id,
                PublicKey = _other.PublicKey,
                AgentVersion = _other.AgentVersion
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmA.StartAsync();
            await swarmB.StartAsync();
            try
            {
                var another = await swarmA.StartListeningAsync(addr);
                Assert.That(peerA.Addresses.Contains(another), Is.True);

                await swarmB.ConnectAsync(another);
                Assert.That(swarmB.KnownPeers.Contains(peerA), Is.True);

                // TODO: Assert.IsTrue(swarmA.KnownPeers.Contains(peerB));

                await swarmA.StopListeningAsync(addr);
                Assert.That(peerA.Addresses.Contains(another), Is.False);
            }
            finally
            {
                await swarmA.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public async Task Listening_IPv4Any()
        {
            var peerA = new Peer
            {
                Id = _self.Id,
                PublicKey = _self.PublicKey,
                AgentVersion = _self.AgentVersion
            };
            MultiAddress addr = "/ip4/0.0.0.0/tcp/0";
            var swarmA = new SwarmService {LocalPeer = peerA};
            var peerB = new Peer
            {
                Id = _other.Id,
                PublicKey = _other.PublicKey,
                AgentVersion = _other.AgentVersion
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmA.StartAsync();
            await swarmB.StartAsync();
            try
            {
                var another = await swarmA.StartListeningAsync(addr);
                Assert.That(peerA.Addresses.Contains(addr), Is.False);
                Assert.That(peerA.Addresses.Contains(another), Is.True);

                await swarmB.ConnectAsync(another);
                Assert.That(swarmB.KnownPeers.Contains(peerA), Is.True);

                // TODO: Assert.IsTrue(swarmA.KnownPeers.Contains(peerB));

                await swarmA.StopListeningAsync(addr);
                Assert.That(0, Is.EqualTo(peerA.Addresses.Count()));
            }
            finally
            {
                await swarmA.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        [Category("IPv6")]
        public async Task Listening_IPv6Any()
        {
            var peerA = new Peer
            {
                Id = _self.Id,
                PublicKey = _self.PublicKey,
                AgentVersion = _self.AgentVersion
            };
            MultiAddress addr = "/ip6/::/tcp/0";
            var swarmA = new SwarmService {LocalPeer = peerA};
            var peerB = new Peer
            {
                Id = _other.Id,
                PublicKey = _other.PublicKey,
                AgentVersion = _other.AgentVersion
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmA.StartAsync();
            await swarmB.StartAsync();
            try
            {
                var another = await swarmA.StartListeningAsync(addr);
                Assert.That(peerA.Addresses.Contains(addr), Is.False);
                Assert.That(peerA.Addresses.Contains(another), Is.True);

                await swarmB.ConnectAsync(another);
                Assert.That(swarmB.KnownPeers.Contains(peerA), Is.True);

                // TODO: Assert.IsTrue(swarmA.KnownPeers.Contains(peerB));

                await swarmA.StopListeningAsync(addr);
                Assert.That(0, Is.EqualTo(peerA.Addresses.Count()));
            }
            finally
            {
                await swarmA.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public void Listening_MissingTransport()
        {
            var peer = new Peer
            {
                Id = _self.Id,
                PublicKey = _self.PublicKey,
                AgentVersion = _self.AgentVersion
            };
            var swarm = new SwarmService {LocalPeer = peer};
            ExceptionAssert.Throws<ArgumentException>(() =>
            {
                var _ = swarm.StartListeningAsync("/ip4/127.0.0.1").Result;
            });
            Assert.That(0, Is.EqualTo(peer.Addresses.Count()));
        }

        [Test]
        public void LocalPeer()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            Assert.That(_self, Is.EqualTo(swarm.LocalPeer))
                ;
            ExceptionAssert.Throws<ArgumentNullException>(() => { swarm.LocalPeer = null; });
            ExceptionAssert.Throws<ArgumentNullException>(() => { swarm.LocalPeer = new Peer {Id = _self.Id}; });
            ExceptionAssert.Throws<ArgumentNullException>(() =>
            {
                swarm.LocalPeer = new Peer {PublicKey = _self.PublicKey};
            });
            ExceptionAssert.Throws<ArgumentException>(() =>
            {
                swarm.LocalPeer = new Peer {Id = _self.Id, PublicKey = _other.PublicKey};
            });

            swarm.LocalPeer = new Peer {Id = _other.Id, PublicKey = _other.PublicKey};
            Assert.That(_other, Is.EqualTo(swarm.LocalPeer));
        }

        [Test]
        public async Task Dial_Peer_No_Address()
        {
            var peer = new Peer
            {
                Id = _mars.PeerId
            };

            var swarm = new SwarmService {LocalPeer = _self};
            await swarm.StartAsync();
            try
            {
                ExceptionAssert.Throws<Exception>(() => { swarm.DialAsync(peer, "/foo/0.42.0").Wait(); });
            }
            finally
            {
                await swarm.StopAsync();
            }
        }

        [Test]
        public async Task Dial_Peer_Not_Listening()
        {
            var peer = new Peer
            {
                Id = _mars.PeerId,
                Addresses = new List<MultiAddress>
                {
                    new MultiAddress($"/ip4/127.0.0.1/tcp/4242/ipfs/{_mars.PeerId}"),
                    new MultiAddress($"/ip4/127.0.0.2/tcp/4242/ipfs/{_mars.PeerId}")
                }
            };

            var swarm = new SwarmService {LocalPeer = _self};
            await swarm.StartAsync();
            try
            {
                ExceptionAssert.Throws<Exception>(() => { swarm.DialAsync(peer, "/foo/0.42.0").Wait(); });
            }
            finally
            {
                await swarm.StopAsync();
            }
        }

        [Test]
        public async Task Dial_Peer_UnknownProtocol()
        {
            var peerB = new Peer
            {
                AgentVersion = "peerB",
                Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                PublicKey =
                    "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            var _ = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarm = new SwarmService {LocalPeer = _self};
            await swarm.StartAsync();
            try
            {
                ExceptionAssert.Throws<Exception>(() => { swarm.DialAsync(peerB, "/foo/0.42.0").Wait(); });
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public async Task Dial_Peer()
        {
            var peerB = new Peer
            {
                AgentVersion = "peerB",
                Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                PublicKey =
                    "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
            };
            var swarmB = new SwarmService {LocalPeer = peerB};
            await swarmB.StartAsync();
            var _ = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");

            var swarm = new SwarmService {LocalPeer = _self};
            await swarm.StartAsync();
            try
            {
                await using (var stream = await swarm.DialAsync(peerB, "/ipfs/id/1.0.0"))
                {
                    Assert.That(stream, Is.Not.Null);
                    Assert.That(stream.CanRead, Is.True);
                    Assert.That(stream.CanWrite, Is.True);
                }
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public void PeerDiscovered()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            var peerCount = 0;
            swarm.PeerDiscovered += (s, e) => { ++peerCount; };
            swarm.RegisterPeerAddress("/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
            swarm.RegisterPeerAddress("/ip4/127.0.0.2/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
            swarm.RegisterPeerAddress("/ip4/127.0.0.3/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
            swarm.RegisterPeerAddress("/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1i");
            swarm.RegisterPeerAddress("/ip4/127.0.0.2/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1i");
            swarm.RegisterPeerAddress("/ip4/127.0.0.3/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1i");
            swarm.RegisterPeer(new Peer {Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1j"});
            swarm.RegisterPeer(new Peer {Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1j"});

            Assert.That(3, Is.EqualTo(peerCount));
        }

        [Test]
        public async Task IsRunning()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            Assert.That(swarm.IsRunning, Is.False);

            await swarm.StartAsync();
            Assert.That(swarm.IsRunning, Is.True);

            await swarm.StopAsync();
            Assert.That(swarm.IsRunning, Is.False);
        }

        [Test]
        public async Task Connect_PrivateNetwork()
        {
            var peerB = new Peer
            {
                AgentVersion = "peerB",
                Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                PublicKey =
                    "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
            };
            var swarmB = new SwarmService {LocalPeer = peerB, NetworkProtector = new OpenNetwork()};
            await swarmB.StartAsync();
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
            Assert.That(peerB.Addresses.Any(), Is.True);

            var swarm = new SwarmService {LocalPeer = _self, NetworkProtector = new OpenNetwork()};
            await swarm.StartAsync();
            try
            {
                var _ = await swarm.ConnectAsync(peerBAddress);
                Assert.That(2, Is.EqualTo(OpenNetwork.Count));
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [Test]
        public void DeregisterPeer()
        {
            var swarm = new SwarmService {LocalPeer = _self};
            swarm.RegisterPeer(_other);
            Assert.That(swarm.KnownPeers.Contains(_other), Is.True);

            Peer removedPeer = null;
            swarm.PeerRemoved += (s, e) => removedPeer = e;
            swarm.DeregisterPeer(_other);
            Assert.That(swarm.KnownPeers.Contains(_other), Is.False);
            Assert.That(_other, Is.EqualTo(removedPeer));
        }

        [Test]
        public void IsAllowed_Peer()
        {
            var swarm = new SwarmService();
            var peer = new Peer
            {
                Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                Addresses = new MultiAddress[]
                {
                    "/ip4/127.0.0.1/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h"
                }
            };

            Assert.That(swarm.IsAllowed(peer), Is.True);

            swarm.BlackList.Add(peer.Addresses.First());
            Assert.That(swarm.IsAllowed(peer), Is.False);

            swarm.BlackList.Clear();
            swarm.BlackList.Add("/p2p/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
            Assert.That(swarm.IsAllowed(peer), Is.False);
        }

        //turn off blacklisting
        //[Test]
        //public void RegisterPeer_BlackListed()
        //{
        //    var swarm = new SwarmService {LocalPeer = _self};
        //    var peer = new Peer
        //    {
        //        Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
        //        Addresses = new MultiAddress[]
        //        {
        //            "/ip4/127.0.0.1/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h"
        //        }
        //    };

        //    swarm.BlackList.Add(peer.Addresses.First());
        //    ExceptionAssert.Throws<Exception>(() => swarm.RegisterPeer(peer));
        //}
    }

    /// <summary>
    ///   A noop private network.
    /// </summary>
    internal sealed class OpenNetwork : INetworkProtector
    {
        public static int Count;

        public Task<Stream> ProtectAsync(PeerConnection connection,
            CancellationToken cancel = default)
        {
            Interlocked.Increment(ref Count);
            return Task.FromResult(connection.Stream);
        }
    }
}
