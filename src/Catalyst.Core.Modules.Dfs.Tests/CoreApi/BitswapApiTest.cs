using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.BlockExchange.Protocols;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Modules.Dfs.BlockExchange;
using Catalyst.Core.Modules.Dfs.BlockExchange.Protocols;
using FluentAssertions;
using Lib.P2P;
using MultiFormats;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class BitswapApiTest
    {
        private IDfs ipfs;
        private IDfs ipfsOther;
        
        public BitswapApiTest(ITestOutputHelper output)
        {
            ipfs = new TestFixture(output).Ipfs;
            ipfsOther = new TestFixture(output).IpfsOther;       
        }

        [Fact]
        public async Task Unwant()
        {
            await ipfs.StartAsync();
            try
            {
                var cts = new CancellationTokenSource();
                var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block 2"));
                Task wantTask = ipfs.Bitswap.GetAsync(block.Id, cts.Token);

                var endTime = DateTime.Now.AddSeconds(10);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        throw new Xunit.Sdk.XunitException("wanted block is missing");
                    }

                    await Task.Delay(100, cts.Token);
                    var w = await ipfs.Bitswap.WantsAsync(cancel: cts.Token);
                    if (w.Contains(block.Id))
                    {
                        break;
                    }
                }

                cts.Cancel();
                await ipfs.Bitswap.UnwantAsync(block.Id, cts.Token);
                var wants = await ipfs.Bitswap.WantsAsync(cancel: cts.Token);
                wants.ToArray().Should().NotContain(block.Id);
                Assert.True(wantTask.IsCanceled);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }
        
        [Fact]
        public async Task Wants()
        {
            await ipfs.StartAsync();
            try
            {
                var cts = new CancellationTokenSource();
                var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block"));
                Task wantTask = ipfs.Bitswap.GetAsync(block.Id, cts.Token);

                var endTime = DateTime.Now.AddSeconds(10);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        throw new Xunit.Sdk.XunitException("wanted block is missing");
                    }
                    
                    await Task.Delay(100);
                    var w = await ipfs.Bitswap.WantsAsync(cancel: cts.Token);
                    if (w.Contains(block.Id))
                        break;
                }
                
                cts.Cancel();
                var wants = await ipfs.Bitswap.WantsAsync(cancel: cts.Token);
                wants.ToArray().Should().NotContain(block.Id);
                Assert.True(wantTask.IsCanceled);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [Fact]
        public async Task OnConnect_Sends_WantList()
        {
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var local = await ipfs.LocalPeer;
                var remote = await ipfsOther.LocalPeer;
                Console.WriteLine($"this at {local.Addresses.First()}");
                Console.WriteLine($"othr at {remote.Addresses.First()}");

                var data = Guid.NewGuid().ToByteArray();
                var cid = new Cid {Hash = MultiHash.ComputeHash(data)};
                var _ = ipfs.Block.GetAsync(cid);
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());

                var endTime = DateTime.Now.AddSeconds(10);
                while (DateTime.Now < endTime)
                {
                    var wants = await ipfsOther.Bitswap.WantsAsync(local.Id);
                    if (wants.Contains(cid))
                        return;
                    await Task.Delay(200);
                }

                throw new Xunit.Sdk.XunitException("want list not sent");
                
                // Assert.Fail("want list not sent");
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();

                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        [Fact]
        public async Task GetsBlock_OnConnect()
        {
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = ipfs.Block.GetAsync(cid, cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cts.Token);
                var block = await getTask;

                Assert.False(getTask.IsCanceled, "task cancelled");
                Assert.False(getTask.IsFaulted, "task faulted");
                Assert.True(getTask.IsCompleted, "task not completed");
                Assert.Equal(cid, block.Id);
                data.Should().BeEquivalentTo(block.DataBytes);

                var otherPeer = await ipfsOther.LocalPeer;
                var ledger = await ipfs.Bitswap.LedgerAsync(otherPeer);
                Assert.Equal(otherPeer, ledger.Peer);
                Assert.NotEqual(0UL, ledger.BlocksExchanged);
                Assert.NotEqual(0UL, ledger.DataReceived);
                Assert.Equal(0UL, ledger.DataSent);
                Assert.True(ledger.IsInDebt);

                // TODO: Timing issue here.  ipfsOther could have sent the block
                // but not updated the stats yet.
#if false
                var localPeer = await ipfs.LocalPeer;
                ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
                Assert.Equal(localPeer, ledger.Peer);
                Assert.NotEqual(0UL, ledger.BlocksExchanged);
                Assert.Equal(0UL, ledger.DataReceived);
                Assert.NotEqual(0UL, ledger.DataSent);
                Assert.False(ledger.IsInDebt);
#endif
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();

                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        [Fact]
        public async Task GetsBlock_OnConnect_Bitswap1()
        {
            var originalProtocols = (await ipfs.BitswapService).Protocols;
            var otherOriginalProtocols = (await ipfsOther.BitswapService).Protocols;

            (await ipfs.BitswapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap1
                {
                    BitswapService = (await ipfs.BitswapService)
                }
            };
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            (await ipfsOther.BitswapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap1 {BitswapService = (await ipfsOther.BitswapService)}
            };
            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = ipfs.Block.GetAsync(cid, cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cts.Token);
                var block = await getTask;

                Assert.False(getTask.IsCanceled, "task cancelled");
                Assert.False(getTask.IsFaulted, "task faulted");
                Assert.True(getTask.IsCompleted, "task not completed");
                Assert.Equal(cid, block.Id);
                Assert.Equal(data, block.DataBytes);

                var otherPeer = await ipfsOther.LocalPeer;
                var ledger = await ipfs.Bitswap.LedgerAsync(otherPeer);
                Assert.Equal(otherPeer, ledger.Peer);
                Assert.NotEqual(0UL, ledger.BlocksExchanged);
                Assert.NotEqual(0UL, ledger.DataReceived);
                Assert.Equal(0UL, ledger.DataSent);
                Assert.True(ledger.IsInDebt);

                // TODO: Timing issue here.  ipfsOther could have sent the block
                // but not updated the stats yet.
#if false
                var localPeer = await ipfs.LocalPeer;
                ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
                Assert.Equal(localPeer, ledger.Peer);
                Assert.NotEqual(0UL, ledger.BlocksExchanged);
                Assert.Equal(0UL, ledger.DataReceived);
                Assert.NotEqual(0UL, ledger.DataSent);
                Assert.False(ledger.IsInDebt);
#endif
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();

                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();

                (await ipfs.BitswapService).Protocols = originalProtocols;
                (await ipfsOther.BitswapService).Protocols = otherOriginalProtocols;
            }
        }

        [Fact]
        public async Task GetsBlock_OnConnect_Bitswap11()
        {
            var originalProtocols = (await ipfs.BitswapService).Protocols;
            var otherOriginalProtocols = (await ipfsOther.BitswapService).Protocols;

            (await ipfs.BitswapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap11 {BitswapService = (await ipfs.BitswapService)}
            };
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            (await ipfsOther.BitswapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap11 {BitswapService = (await ipfsOther.BitswapService)}
            };
            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = ipfs.Block.GetAsync(cid, cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cts.Token);
                var block = await getTask;

                Assert.False(getTask.IsCanceled, "task cancelled");
                Assert.False(getTask.IsFaulted, "task faulted");
                Assert.True(getTask.IsCompleted, "task not completed");
                Assert.Equal(cid, block.Id);
                Assert.Equal(data, block.DataBytes);

                var otherPeer = await ipfsOther.LocalPeer;
                var ledger = await ipfs.Bitswap.LedgerAsync(otherPeer);
                Assert.Equal(otherPeer, ledger.Peer);
                Assert.NotEqual(0UL, ledger.BlocksExchanged);
                Assert.NotEqual(0UL, ledger.DataReceived);
                Assert.Equal(0UL, ledger.DataSent);
                Assert.True(ledger.IsInDebt);

                // TODO: Timing issue here.  ipfsOther could have sent the block
                // but not updated the stats yet.
#if false
                var localPeer = await ipfs.LocalPeer;
                ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
                Assert.Equal(localPeer, ledger.Peer);
                Assert.NotEqual(0UL, ledger.BlocksExchanged);
                Assert.Equal(0UL, ledger.DataReceived);
                Assert.NotEqual(0UL, ledger.DataSent);
                Assert.False(ledger.IsInDebt);
#endif
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();

                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();

                (await ipfs.BitswapService).Protocols = originalProtocols;
                (await ipfsOther.BitswapService).Protocols = otherOriginalProtocols;
            }
        }

        [Fact]
        public async Task GetsBlock_OnRequest()
        {
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var cts = new CancellationTokenSource(10000);
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data, cancel: cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cancel: cts.Token);

                var block = await ipfs.Block.GetAsync(cid, cancel: cts.Token);
                Assert.Equal(cid, block.Id);
                Assert.Equal(data, block.DataBytes);
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();
                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        [Fact]
        public async Task GetsBlock_Cidv1()
        {
            await ipfs.StartAsync();
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data, "raw", "sha2-512");

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());

                var cts = new CancellationTokenSource(3000);
                var block = await ipfs.Block.GetAsync(cid, cts.Token);
                Assert.Equal(cid, block.Id);
                Assert.Equal(data, block.DataBytes);
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();
            }
        }

        [Fact]
        public async Task GetBlock_Timeout()
        {
            var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block"));

            await ipfs.StartAsync();
            try
            {
                var cts = new CancellationTokenSource(300);
                ExceptionAssert.Throws<TaskCanceledException>(() =>
                {
                    var _ = ipfs.Bitswap.GetAsync(block.Id, cts.Token).Result;
                });

                Assert.Equal(0, (await ipfs.Bitswap.WantsAsync()).Count());
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [Fact]
        public async Task PeerLedger()
        {
            await ipfs.StartAsync();
            try
            {
                var peer = await ipfsOther.LocalPeer;
                var ledger = await ipfs.Bitswap.LedgerAsync(peer);
                Assert.NotNull(ledger);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }
    }
}
