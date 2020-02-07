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
using System.Text;
using System.Threading;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Dfs.BlockExchange;
using Catalyst.Core.Modules.Hashing;
using FluentAssertions;
using Lib.P2P;
using MultiFormats.Registry;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.BlockExchange
{
    public sealed class BitSwapTest
    {
        private readonly Peer _self = new Peer
        {
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        public BitSwapTest()
        {
            new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
        }

        [Fact]
        public void WantList()
        {
            var bitSwap = new BitSwapService {SwarmService = new SwarmService {LocalPeer = _self}};
            Assert.Equal(0, bitSwap.PeerWants(_self.Id).Count());

            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var _ = bitSwap.WantAsync(cid, _self.Id, cancel.Token);
            bitSwap.PeerWants(_self.Id).ToArray().Should().Contain(cid);

            bitSwap.Unwant(cid);
            bitSwap.PeerWants(_self.Id).ToArray().Should().NotContain(cid);
        }

        [Fact]
        public void Want_Cancel()
        {
            var bitSwap = new BitSwapService {SwarmService = new SwarmService {LocalPeer = _self}};
            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var task = bitSwap.WantAsync(cid, _self.Id, cancel.Token);
            bitSwap.PeerWants(_self.Id).ToArray().Should().Contain(cid);

            cancel.Cancel();
            Assert.True(task.IsCanceled);

            bitSwap.PeerWants(_self.Id).ToArray().Should().NotContain(cid);
        }

        [Fact]
        public void Block_Needed()
        {
            var bitSwap = new BitSwapService {SwarmService = new SwarmService {LocalPeer = _self}};
            var cid1 = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block y")).Id;
            var cid2 = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block z")).Id;
            var cancel = new CancellationTokenSource();
            var callCount = 0;
            
            bitSwap.BlockNeeded += (s, e) =>
            {
                Assert.True(cid1 == e.Id || cid2 == e.Id);
                ++callCount;
            };
            try
            {
                bitSwap.WantAsync(cid1, _self.Id, cancel.Token);
                bitSwap.WantAsync(cid1, _self.Id, cancel.Token);
                bitSwap.WantAsync(cid2, _self.Id, cancel.Token);
                bitSwap.WantAsync(cid2, _self.Id, cancel.Token);
                Assert.Equal(2, callCount);
            }
            finally
            {
                cancel.Cancel();
            }
        }

        [Fact]
        public void WantUnwantTests()
        {
            var bitSwapService = new BitSwapService {SwarmService = new SwarmService {LocalPeer = _self}};
            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var task = bitSwapService.WantAsync(cid, _self.Id, cancel.Token);
            
            bitSwapService.PeerWants(_self.Id).ToArray().Should().Contain(cid);

            bitSwapService.Unwant(cid);
            Assert.True(task.IsCanceled);
            bitSwapService.PeerWants(_self.Id).ToArray().Should().NotContain(cid);
        }

        [Fact]
        public void Found()
        {
            var bitSwapService = new BitSwapService {SwarmService = new SwarmService {LocalPeer = _self}};
            Assert.Equal(0, bitSwapService.PeerWants(_self.Id).Count());

            var a = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block a"));
            var b = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block b"));
            var cancel = new CancellationTokenSource();
            var task = bitSwapService.WantAsync(a.Id, _self.Id, cancel.Token);
            Assert.False(task.IsCompleted);
            bitSwapService.PeerWants(_self.Id).ToArray().Should().Contain(a.Id);

            bitSwapService.Found(b);
            Assert.False(task.IsCompleted);
            bitSwapService.PeerWants(_self.Id).ToArray().Should().Contain(a.Id);

            bitSwapService.Found(a);
            Assert.True(task.IsCompleted);
            bitSwapService.PeerWants(_self.Id).ToArray().Should().NotContain(a.Id);
            a.DataBytes.Should().Contain(task.Result.DataBytes);
        }

        [Fact]
        public void Found_Count()
        {
            var bitSwapService = new BitSwapService {SwarmService = new SwarmService {LocalPeer = _self}};

            var a = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block a"));
            Assert.Equal(0, bitSwapService.Found(a));

            var cancel = new CancellationTokenSource();
            var task1 = bitSwapService.WantAsync(a.Id, _self.Id, cancel.Token);
            var task2 = bitSwapService.WantAsync(a.Id, _self.Id, cancel.Token);
            Assert.Equal(2, bitSwapService.Found(a));

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
        }
    }
}
