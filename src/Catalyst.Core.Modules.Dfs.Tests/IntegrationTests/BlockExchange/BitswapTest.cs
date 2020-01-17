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
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Dfs.BlockExchange;
using Catalyst.Core.Modules.Hashing;
using FluentAssertions;
using Lib.P2P;
using MultiFormats.Registry;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.BlockExchange
{
    public class BitswapTest
    {
        private readonly IHashProvider _hashProvider;

        Peer self = new Peer
        {
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        public BitswapTest()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
        }

        [Fact]
        public void WantList()
        {
            var bitswap = new BitSwapService {SwarmService = new SwarmService() {LocalPeer = self}};
            Assert.Equal(0, bitswap.PeerWants(self.Id).Count());

            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var task = bitswap.WantAsync(cid, self.Id, cancel.Token);
            bitswap.PeerWants(self.Id).ToArray().Should().Contain(cid);

            // Assert.Contains(bitswap.PeerWants(self.Id).ToArray(), cid);

            bitswap.Unwant(cid);
            bitswap.PeerWants(self.Id).ToArray().Should().NotContain(cid);

            // Assert.DoesNotContain(bitswap.PeerWants(self.Id).ToArray(), cid);
        }

        [Fact]
        public void Want_Cancel()
        {
            var bitswap = new BitSwapService {SwarmService = new SwarmService() {LocalPeer = self}};
            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var task = bitswap.WantAsync(cid, self.Id, cancel.Token);

            // Assert.Contains(bitswap.PeerWants(self.Id).ToArray(), cid);
            bitswap.PeerWants(self.Id).ToArray().Should().Contain(cid);

            cancel.Cancel();
            Assert.True(task.IsCanceled);
            
            // Assert.DoesNotContain(bitswap.PeerWants(self.Id).ToArray(), cid);

            bitswap.PeerWants(self.Id).ToArray().Should().NotContain(cid);
        }

        [Fact]
        public void Block_Needed()
        {
            var bitswap = new BitSwapService {SwarmService = new SwarmService() {LocalPeer = self}};
            var cid1 = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block y")).Id;
            var cid2 = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block z")).Id;
            var cancel = new CancellationTokenSource();
            int callCount = 0;
            bitswap.BlockNeeded += (s, e) =>
            {
                Assert.True(cid1 == e.Id || cid2 == e.Id);
                ++callCount;
            };
            try
            {
                bitswap.WantAsync(cid1, self.Id, cancel.Token);
                bitswap.WantAsync(cid1, self.Id, cancel.Token);
                bitswap.WantAsync(cid2, self.Id, cancel.Token);
                bitswap.WantAsync(cid2, self.Id, cancel.Token);
                Assert.Equal(2, callCount);
            }
            finally
            {
                cancel.Cancel();
            }
        }

        [Fact]
        public void Want_Unwant()
        {
            var bitswap = new BitSwapService {SwarmService = new SwarmService() {LocalPeer = self}};
            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var task = bitswap.WantAsync(cid, self.Id, cancel.Token);
            
            bitswap.PeerWants(self.Id).ToArray().Should().Contain(cid);

            // Assert.Contains(bitswap.PeerWants(self.Id).ToArray(), cid);

            bitswap.Unwant(cid);
            Assert.True(task.IsCanceled);
            
            // Assert.DoesNotContain(bitswap.PeerWants(self.Id).ToArray(), cid);
            bitswap.PeerWants(self.Id).ToArray().Should().NotContain(cid);
        }

        [Fact]
        public void Found()
        {
            var bitswap = new BitSwapService {SwarmService = new SwarmService() {LocalPeer = self}};
            Assert.Equal(0, bitswap.PeerWants(self.Id).Count());

            var a = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block a"));
            var b = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block b"));
            var cancel = new CancellationTokenSource();
            var task = bitswap.WantAsync(a.Id, self.Id, cancel.Token);
            Assert.False(task.IsCompleted);
            bitswap.PeerWants(self.Id).ToArray().Should().Contain(a.Id);

            bitswap.Found(b);
            Assert.False(task.IsCompleted);
            bitswap.PeerWants(self.Id).ToArray().Should().Contain(a.Id);

            bitswap.Found(a);
            Assert.True(task.IsCompleted);
            bitswap.PeerWants(self.Id).ToArray().Should().NotContain(a.Id);
            a.DataBytes.Should().Contain(task.Result.DataBytes);
        }

        [Fact]
        public void Found_Count()
        {
            var bitswap = new BitSwapService {SwarmService = new SwarmService {LocalPeer = self}};

            var a = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block a"));
            Assert.Equal(0, bitswap.Found(a));

            var cancel = new CancellationTokenSource();
            var task1 = bitswap.WantAsync(a.Id, self.Id, cancel.Token);
            var task2 = bitswap.WantAsync(a.Id, self.Id, cancel.Token);
            Assert.Equal(2, bitswap.Found(a));

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
        }
    }
}
