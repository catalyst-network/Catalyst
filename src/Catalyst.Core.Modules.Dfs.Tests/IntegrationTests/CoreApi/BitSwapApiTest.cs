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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.BlockExchange.Protocols;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Dfs.BlockExchange.Protocols;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using FluentAssertions;
using Lib.P2P;
using MultiFormats;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public sealed class BitSwapApiTest
    {
        private IDfsService _dfsService;
        private IDfsService _dfsServiceOther;

        [SetUp]
        public void Init()
        {
            var fileSystem1 = Substitute.For<IFileSystem>();
            fileSystem1.GetCatalystDataDir().Returns(new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,
                $"dfs1-_{DateTime.Now:yyMMddHHmmssffff}")));

            var fileSystem2 = Substitute.For<IFileSystem>();
            fileSystem2.GetCatalystDataDir().Returns(new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,
                $"dfs2-_{DateTime.Now:yyMMddHHmmssffff}")));

            _dfsService = TestDfs.GetTestDfs(fileSystem1);
            _dfsServiceOther = TestDfs.GetTestDfs(fileSystem2);
        }

        /// <summary>
        ///     @TODO this assert is commented out
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [Test]
        public async Task UnWant()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetCatalystDataDir().Returns(new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,
                $"dfs1-_{DateTime.Now:yyMMddHHmmssffff}")));

            var dfsService = TestDfs.GetTestDfs(fileSystem);
            await dfsService.StartAsync();
            
            try
            {
                var cts = new CancellationTokenSource();
                var block = new DagNode(Encoding.UTF8.GetBytes("BitSwapApiTest unknown block 2"));
                var wantTask = dfsService.BitSwapApi.GetAsync(block.Id, cts.Token).ConfigureAwait(false);

                var endTime = DateTime.Now.AddSeconds(10);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        throw new Exception("wanted block is missing");
                    }

                    await Task.Delay(100, cts.Token);
                    var w = await dfsService.BitSwapApi.WantsAsync(cancel: cts.Token);
                    if (w.Contains(block.Id))
                    {
                        break;
                    }
                }

                cts.Cancel();
                dfsService.BitSwapApi.UnWant(block.Id, cts.Token);
                var wants = await dfsService.BitSwapApi.WantsAsync(cancel: cts.Token);
                wants.ToArray().Should().NotContain(block.Id);

                //Race condition between threads
                // Assert.True(wantTask.GetAwaiter().IsCompleted);
            }
            finally
            {
                await dfsService.StopAsync();
            }
        }
        
        [Test]
        public async Task Wants()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetCatalystDataDir().Returns(new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,
                $"dfs1-_{DateTime.Now:yyMMddHHmmssffff}")));
            
            var dfsService = TestDfs.GetTestDfs(fileSystem);
            await dfsService.StartAsync();
            
            try
            {
                var cts = new CancellationTokenSource();
                var block = new DagNode(Encoding.UTF8.GetBytes("BitSwapApiTest unknown block"));
                var wantTask = dfsService.BitSwapApi.GetAsync(block.Id, cts.Token);

                var endTime = DateTime.Now.AddSeconds(10);
                while (true)
                {
                    if (DateTime.Now > endTime)
                    {
                        throw new Exception("wanted block is missing");
                    }
                    
                    await Task.Delay(100, cts.Token);
                    var w = await dfsService.BitSwapApi.WantsAsync(cancel: cts.Token);
                    if (w.Contains(block.Id))
                    {
                        break;
                    }
                }
                
                cts.Cancel();
                var wants = await _dfsService.BitSwapApi.WantsAsync(cancel: cts.Token);
                wants.ToArray().Should().NotContain(block.Id);

                // Race condition between threads
                // Assert.True(wantTask.IsCanceled);
            }
            finally
            {
                await _dfsService.StopAsync();
            }
        }

        [Test]
        public async Task OnConnect_Sends_WantList()
        {
            _dfsService.Options.Discovery.DisableMdns = true;
            _dfsService.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await _dfsService.StartAsync();

            _dfsServiceOther.Options.Discovery.DisableMdns = true;
            _dfsServiceOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await _dfsServiceOther.StartAsync();
            
            try
            {
                var local = _dfsService.LocalPeer;
                var remote = _dfsServiceOther.LocalPeer;
                TestContext.WriteLine($"this at {local.Addresses.First()}");
                TestContext.WriteLine($"other at {remote.Addresses.First()}");

                var data = Guid.NewGuid().ToByteArray();
                var cid = new Cid
                {
                    Hash = MultiHash.ComputeHash(data)
                };
                
                var _ = _dfsService.BlockApi.GetAsync(cid);
                await _dfsService.SwarmApi.ConnectAsync(remote.Addresses.First());

                var endTime = DateTime.Now.AddSeconds(10);
                while (DateTime.Now < endTime)
                {
                    var wants = await _dfsServiceOther.BitSwapApi.WantsAsync(local.Id);
                    if (wants.Contains(cid))
                    {
                        return;
                    }
                    
                    await Task.Delay(200);
                }

                throw new Exception("want list not sent");
            }
            finally
            {
                await _dfsServiceOther.StopAsync();
                await _dfsService.StopAsync();

                _dfsService.Options.Discovery = new DiscoveryOptions();
                _dfsServiceOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        [Test]
        public async Task GetsBlock_OnConnect()
        {
            _dfsService.Options.Discovery.DisableMdns = true;
            _dfsService.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await _dfsService.StartAsync();

            _dfsServiceOther.Options.Discovery.DisableMdns = true;
            _dfsServiceOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await _dfsServiceOther.StartAsync();
            
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await _dfsServiceOther.BlockApi.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = _dfsService.BlockApi.GetAsync(cid, cts.Token);

                var remote = _dfsServiceOther.LocalPeer;
                await _dfsService.SwarmApi.ConnectAsync(remote.Addresses.First(), cts.Token);
                var block = await getTask;

                Assert.False(getTask.IsCanceled, "task cancelled");
                Assert.False(getTask.IsFaulted, "task faulted");
                Assert.True(getTask.IsCompleted, "task not completed");
                Assert.AreEqual(cid, block.Id);
                
                data.Should().BeEquivalentTo(block.DataBytes);

                var otherPeer = _dfsServiceOther.LocalPeer;
                var ledger = _dfsService.BitSwapApi.GetBitSwapLedger(otherPeer, cts.Token);
                
                Assert.AreEqual(otherPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreNotEqual(0UL, ledger.DataReceived);
                Assert.AreEqual(0UL, ledger.DataSent);
                Assert.True(ledger.IsInDebt);

                // TODO: Timing issue here.  ipfsOther could have sent the block
                // but not updated the stats yet.
#if false
                var localPeer = await ipfs.LocalPeer;
                ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
                Assert.AreEqual(localPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreEqual(0UL, ledger.DataReceived);
                Assert.AreNotEqual(0UL, ledger.DataSent);
                Assert.False(ledger.IsInDebt);
#endif
            }
            finally
            {
                await _dfsServiceOther.StopAsync();
                await _dfsService.StopAsync();

                _dfsService.Options.Discovery = new DiscoveryOptions();
                _dfsServiceOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        //         [Test]
        //         public async Task GetsBlock_OnConnect_BitSwap1()
        //         {
        //             var originalProtocols = (_dfsService.BitSwapService).Protocols;
        //             var otherOriginalProtocols = (_dfsServiceOther.BitSwapService).Protocols;
        //
        //             (_dfsService.BitSwapService).Protocols = new IBitswapProtocol[]
        //             {
        //                 new Bitswap1
        //                 {
        //                     BitswapService = _dfsService.BitSwapService
        //                 }
        //             };
        //             
        //             _dfsService.Options.Discovery.DisableMdns = true;
        //             _dfsService.Options.Discovery.BootstrapPeers = new MultiAddress[0];
        //             await _dfsService.StartAsync();
        //
        //             (_dfsServiceOther.BitSwapService).Protocols = new IBitswapProtocol[]
        //             {
        //                 new Bitswap1
        //                 {
        //                     BitswapService = _dfsServiceOther.BitSwapService
        //                 }
        //             };
        //             
        //             _dfsServiceOther.Options.Discovery.DisableMdns = true;
        //             _dfsServiceOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
        //             await _dfsServiceOther.StartAsync();
        //             
        //             try
        //             {
        //                 var data = Guid.NewGuid().ToByteArray();
        //                 var cid = await _dfsServiceOther.BlockApi.PutAsync(data);
        //
        //                 var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        //                 var getTask = _dfsService.BlockApi.GetAsync(cid, cts.Token);
        //
        //                 var remote = _dfsServiceOther.LocalPeer;
        //                 await _dfsService.SwarmApi.ConnectAsync(remote.Addresses.First(), cts.Token);
        //                 var block = await getTask;
        //
        //                 Assert.False(getTask.IsCanceled, "task cancelled");
        //                 Assert.False(getTask.IsFaulted, "task faulted");
        //                 Assert.True(getTask.IsCompleted, "task not completed");
        //                 Assert.AreEqual(cid, block.Id);
        //                 Assert.AreEqual(data, block.DataBytes);
        //
        //                 var otherPeer = _dfsServiceOther.LocalPeer;
        //                 var ledger = await _dfsService.BitSwapApi.LedgerAsync(otherPeer, cts.Token);
        //                 Assert.AreEqual(otherPeer, ledger.Peer);
        //                 Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
        //                 Assert.AreNotEqual(0UL, ledger.DataReceived);
        //                 Assert.AreEqual(0UL, ledger.DataSent);
        //                 Assert.True(ledger.IsInDebt);
        //
        //                 // TODO: Timing issue here.  ipfsOther could have sent the block
        //                 // but not updated the stats yet.
        // #if false
        //                 var localPeer = await ipfs.LocalPeer;
        //                 ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
        //                 Assert.AreEqual(localPeer, ledger.Peer);
        //                 Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
        //                 Assert.AreEqual(0UL, ledger.DataReceived);
        //                 Assert.AreNotEqual(0UL, ledger.DataSent);
        //                 Assert.False(ledger.IsInDebt);
        // #endif
        //             }
        //             finally
        //             {
        //                 await _dfsServiceOther.StopAsync();
        //                 await _dfsService.StopAsync();
        //
        //                 _dfsService.Options.Discovery = new DiscoveryOptions();
        //                 _dfsServiceOther.Options.Discovery = new DiscoveryOptions();
        //
        //                 (_dfsService.BitSwapService).Protocols = originalProtocols;
        //                 (_dfsServiceOther.BitSwapService).Protocols = otherOriginalProtocols;
        //             }
        //         }

        [Test]
        public async Task GetsBlock_OnConnect_BitSwap11()
        {
            var originalProtocols = (_dfsService.BitSwapService).Protocols;
            var otherOriginalProtocols = (_dfsServiceOther.BitSwapService).Protocols;

            (_dfsService.BitSwapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap11
                {
                    BitswapService = _dfsService.BitSwapService
                }
            };
            
            _dfsService.Options.Discovery.DisableMdns = true;
            _dfsService.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await _dfsService.StartAsync();

            (_dfsServiceOther.BitSwapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap11
                {
                    BitswapService = _dfsServiceOther.BitSwapService
                }
            };
            _dfsServiceOther.Options.Discovery.DisableMdns = true;
            _dfsServiceOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await _dfsServiceOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await _dfsServiceOther.BlockApi.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = _dfsService.BlockApi.GetAsync(cid, cts.Token);

                var remote = _dfsServiceOther.LocalPeer;
                await _dfsService.SwarmApi.ConnectAsync(remote.Addresses.First(), cts.Token);
                var block = await getTask;

                Assert.False(getTask.IsCanceled, "task cancelled");
                Assert.False(getTask.IsFaulted, "task faulted");
                Assert.True(getTask.IsCompleted, "task not completed");
                Assert.AreEqual(cid, block.Id);
                Assert.AreEqual(data, block.DataBytes);

                var otherPeer = _dfsServiceOther.LocalPeer;
                var ledger = _dfsService.BitSwapApi.GetBitSwapLedger(otherPeer, cts.Token);
                Assert.AreEqual(otherPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreNotEqual(0UL, ledger.DataReceived);
                Assert.AreEqual(0UL, ledger.DataSent);
                Assert.True(ledger.IsInDebt);

                // TODO: Timing issue here.  ipfsOther could have sent the block
                // but not updated the stats yet.
#if false
                var localPeer = await ipfs.LocalPeer;
                ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
                Assert.AreEqual(localPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreEqual(0UL, ledger.DataReceived);
                Assert.AreNotEqual(0UL, ledger.DataSent);
                Assert.False(ledger.IsInDebt);
#endif
            }
            finally
            {
                await _dfsServiceOther.StopAsync();
                await _dfsService.StopAsync();

                _dfsService.Options.Discovery = new DiscoveryOptions();
                _dfsServiceOther.Options.Discovery = new DiscoveryOptions();

                (_dfsService.BitSwapService).Protocols = originalProtocols;
                (_dfsServiceOther.BitSwapService).Protocols = otherOriginalProtocols;
            }
        }

        [Test]
        public async Task GetsBlock_OnRequest()
        {
            _dfsService.Options.Discovery.DisableMdns = true;
            _dfsService.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await _dfsService.StartAsync();

            _dfsServiceOther.Options.Discovery.DisableMdns = true;
            _dfsServiceOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await _dfsServiceOther.StartAsync();
            
            try
            {
                var cts = new CancellationTokenSource(10000);
                var data = Guid.NewGuid().ToByteArray();
                var cid = await _dfsServiceOther.BlockApi.PutAsync(data, cancel: cts.Token);

                var remote = _dfsServiceOther.LocalPeer;
                await _dfsService.SwarmApi.ConnectAsync(remote.Addresses.First(), cts.Token);

                var block = await _dfsService.BlockApi.GetAsync(cid, cts.Token);
                
                Assert.AreEqual(cid, block.Id);
                Assert.AreEqual(data, block.DataBytes);
            }
            finally
            {
                await _dfsServiceOther.StopAsync();
                await _dfsService.StopAsync();
                _dfsService.Options.Discovery = new DiscoveryOptions();
                _dfsServiceOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        [Test]
        public async Task GetsBlock_CidV1()
        {
            await _dfsService.StartAsync();
            await _dfsServiceOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await _dfsServiceOther.BlockApi.PutAsync(data, "raw"); // @TODO get this from a test prop so we can test against multiple hash algos

                var remote = _dfsServiceOther.LocalPeer;
                await _dfsService.SwarmApi.ConnectAsync(remote.Addresses.First());

                var cts = new CancellationTokenSource(3000);
                var block = await _dfsService.BlockApi.GetAsync(cid, cts.Token);
                
                Assert.AreEqual(cid, block.Id);
                Assert.AreEqual(data, block.DataBytes);
            }
            finally
            {
                await _dfsServiceOther.StopAsync();
                await _dfsService.StopAsync();
            }
        }

        [Test]
        public async Task GetBlock_Timeout()
        {
            var block = new DagNode(Encoding.UTF8.GetBytes("BitSwapApiTest unknown block"));
            await _dfsService.StartAsync();
            
            try
            {
                var cts = new CancellationTokenSource(300);
                ExceptionAssert.Throws<TaskCanceledException>(() =>
                {
                    var _ = _dfsService.BitSwapApi.GetAsync(block.Id, cts.Token).Result;
                });

                Assert.AreEqual(0, (await _dfsService.BitSwapApi.WantsAsync(cancel: cts.Token)).Count());
            }
            finally
            {
                await _dfsService.StopAsync();
            }
        }

        [Test]
        public async Task PeerLedger()
        {
            await _dfsService.StartAsync();
         
            try
            {
                var peer = _dfsServiceOther.LocalPeer;
                var cts = new CancellationTokenSource(300);
                var ledger = _dfsService.BitSwapApi.GetBitSwapLedger(peer, cts.Token);
                Assert.NotNull(ledger);
            }
            finally
            {
                await _dfsService.StopAsync();
            }
        }
    }
}
