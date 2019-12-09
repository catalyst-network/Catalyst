using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.BlockExchange.Protocols;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Dfs.BlockExchange.Protocols;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using FluentAssertions;
using Lib.P2P;
using MultiFormats;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class BitswapApiTest
    {
        private IDfsService ipfs;
        private IDfsService ipfsOther;
        
        public BitswapApiTest(ITestOutputHelper output)
        {
            ipfs = TestDfs.GetTestDfs(output, "ipfs-1");
            ipfsOther = TestDfs.GetTestDfs(output, "ipfs-2");
        }

        [Fact]
        public async Task Unwant()
        {
            await ipfs.StartAsync();
            try
            {
                var cts = new CancellationTokenSource();
                var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block 2"));
                Task wantTask = ipfs.BitSwapApi.GetAsync(block.Id, cts.Token);

                var endTime = DateTime.Now.AddSeconds(10);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        throw new Xunit.Sdk.XunitException("wanted block is missing");
                    }

                    await Task.Delay(100, cts.Token);
                    var w = await ipfs.BitSwapApi.WantsAsync(cancel: cts.Token);
                    if (w.Contains(block.Id))
                    {
                        break;
                    }
                }

                cts.Cancel();
                await ipfs.BitSwapApi.UnWantAsync(block.Id, cts.Token);
                var wants = await ipfs.BitSwapApi.WantsAsync(cancel: cts.Token);
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
                Task wantTask = ipfs.BitSwapApi.GetAsync(block.Id, cts.Token);

                var endTime = DateTime.Now.AddSeconds(10);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        throw new Xunit.Sdk.XunitException("wanted block is missing");
                    }
                    
                    await Task.Delay(100, cts.Token);
                    var w = await ipfs.BitSwapApi.WantsAsync(cancel: cts.Token);
                    if (w.Contains(block.Id))
                        break;
                }
                
                cts.Cancel();
                var wants = await ipfs.BitSwapApi.WantsAsync(cancel: cts.Token);
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
                var _ = ipfs.BlockApi.GetAsync(cid);
                await ipfs.SwarmApi.ConnectAsync(remote.Addresses.First());

                var endTime = DateTime.Now.AddSeconds(10);
                while (DateTime.Now < endTime)
                {
                    var wants = await ipfsOther.BitSwapApi.WantsAsync(local.Id);
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
                var cid = await ipfsOther.BlockApi.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = ipfs.BlockApi.GetAsync(cid, cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.SwarmApi.ConnectAsync(remote.Addresses.First(), cts.Token);
                var block = await getTask;

                Assert.False(getTask.IsCanceled, "task cancelled");
                Assert.False(getTask.IsFaulted, "task faulted");
                Assert.True(getTask.IsCompleted, "task not completed");
                Assert.Equal(cid, block.Id);
                data.Should().BeEquivalentTo(block.DataBytes);

                var otherPeer = await ipfsOther.LocalPeer;
                var ledger = await ipfs.BitSwapApi.LedgerAsync(otherPeer);
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
            var originalProtocols = (await ipfs.BitSwapService).Protocols;
            var otherOriginalProtocols = (await ipfsOther.BitSwapService).Protocols;

            (await ipfs.BitSwapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap1
                {
                    BitswapService = (await ipfs.BitSwapService)
                }
            };
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            (await ipfsOther.BitSwapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap1 {BitswapService = (await ipfsOther.BitSwapService)}
            };
            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.BlockApi.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = ipfs.BlockApi.GetAsync(cid, cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.SwarmApi.ConnectAsync(remote.Addresses.First(), cts.Token);
                var block = await getTask;

                Assert.False(getTask.IsCanceled, "task cancelled");
                Assert.False(getTask.IsFaulted, "task faulted");
                Assert.True(getTask.IsCompleted, "task not completed");
                Assert.Equal(cid, block.Id);
                Assert.Equal(data, block.DataBytes);

                var otherPeer = await ipfsOther.LocalPeer;
                var ledger = await ipfs.BitSwapApi.LedgerAsync(otherPeer);
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

                (await ipfs.BitSwapService).Protocols = originalProtocols;
                (await ipfsOther.BitSwapService).Protocols = otherOriginalProtocols;
            }
        }

        [Fact]
        public async Task GetsBlock_OnConnect_Bitswap11()
        {
            var originalProtocols = (await ipfs.BitSwapService).Protocols;
            var otherOriginalProtocols = (await ipfsOther.BitSwapService).Protocols;

            (await ipfs.BitSwapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap11 {BitswapService = (await ipfs.BitSwapService)}
            };
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            (await ipfsOther.BitSwapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap11 {BitswapService = (await ipfsOther.BitSwapService)}
            };
            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.BlockApi.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = ipfs.BlockApi.GetAsync(cid, cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.SwarmApi.ConnectAsync(remote.Addresses.First(), cts.Token);
                var block = await getTask;

                Assert.False(getTask.IsCanceled, "task cancelled");
                Assert.False(getTask.IsFaulted, "task faulted");
                Assert.True(getTask.IsCompleted, "task not completed");
                Assert.Equal(cid, block.Id);
                Assert.Equal(data, block.DataBytes);

                var otherPeer = await ipfsOther.LocalPeer;
                var ledger = await ipfs.BitSwapApi.LedgerAsync(otherPeer);
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

                (await ipfs.BitSwapService).Protocols = originalProtocols;
                (await ipfsOther.BitSwapService).Protocols = otherOriginalProtocols;
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
                var cid = await ipfsOther.BlockApi.PutAsync(data, cancel: cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.SwarmApi.ConnectAsync(remote.Addresses.First(), cancel: cts.Token);

                var block = await ipfs.BlockApi.GetAsync(cid, cancel: cts.Token);
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
                var cid = await ipfsOther.BlockApi.PutAsync(data, "raw", "sha2-512");

                var remote = await ipfsOther.LocalPeer;
                await ipfs.SwarmApi.ConnectAsync(remote.Addresses.First());

                var cts = new CancellationTokenSource(3000);
                var block = await ipfs.BlockApi.GetAsync(cid, cts.Token);
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
                    var _ = ipfs.BitSwapApi.GetAsync(block.Id, cts.Token).Result;
                });

                Assert.Equal(0, (await ipfs.BitSwapApi.WantsAsync()).Count());
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
                var ledger = await ipfs.BitSwapApi.LedgerAsync(peer);
                Assert.NotNull(ledger);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }
    }
}
