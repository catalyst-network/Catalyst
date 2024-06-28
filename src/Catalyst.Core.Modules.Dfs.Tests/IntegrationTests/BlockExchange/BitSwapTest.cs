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
using Catalyst.TestUtils;
using FluentAssertions;
using Lib.P2P;
using MultiFormats.Registry;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.BlockExchange
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
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
            new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
        }

        [Test]
        public void WantList()
        {
            var bitSwapService = new BitSwapService(new SwarmService(_self));
            Assert.That(bitSwapService.PeerWants(_self.Id).Count(), Is.EqualTo(0));

            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var _ = bitSwapService.WantAsync(cid, _self.Id, cancel.Token);
            bitSwapService.PeerWants(_self.Id).ToArray().Should().Contain(cid);

            bitSwapService.Unwant(cid);
            bitSwapService.PeerWants(_self.Id).ToArray().Should().NotContain(cid);
        }

        [Test]
        public void Want_Cancel()
        {
            var bitSwapService = new BitSwapService(new SwarmService(_self));
            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var task = bitSwapService.WantAsync(cid, _self.Id, cancel.Token);
            bitSwapService.PeerWants(_self.Id).ToArray().Should().Contain(cid);

            cancel.Cancel();
            Assert.That(task.IsCanceled, Is.True);

            bitSwapService.PeerWants(_self.Id).ToArray().Should().NotContain(cid);
        }

        [Test]
        public void Block_Needed()
        {
            var bitSwapService = new BitSwapService(new SwarmService(_self));
            var cid1 = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block y")).Id;
            var cid2 = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block z")).Id;
            var cancel = new CancellationTokenSource();
            var callCount = 0;

            bitSwapService.BlockNeeded += (s, e) =>
            {
                Assert.That(cid1 == e.Id || cid2 == e.Id, Is.True);
                ++callCount;
            };
            try
            {
                bitSwapService.WantAsync(cid1, _self.Id, cancel.Token);
                bitSwapService.WantAsync(cid1, _self.Id, cancel.Token);
                bitSwapService.WantAsync(cid2, _self.Id, cancel.Token);
                bitSwapService.WantAsync(cid2, _self.Id, cancel.Token);
                Assert.That(callCount, Is.EqualTo(2));
            }
            finally
            {
                cancel.Cancel();
            }
        }

        [Test]
        public void WantUnwantTests()
        {
            var bitSwapService = new BitSwapService(new SwarmService(_self));
            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var task = bitSwapService.WantAsync(cid, _self.Id, cancel.Token);
            
            bitSwapService.PeerWants(_self.Id).ToArray().Should().Contain(cid);

            bitSwapService.Unwant(cid);
            Assert.That(task.IsCanceled, Is.True);
            bitSwapService.PeerWants(_self.Id).ToArray().Should().NotContain(cid);
        }

        [Test]
        public void Found()
        {
            var bitSwapService = new BitSwapService(new SwarmService(_self));
            Assert.That(bitSwapService.PeerWants(_self.Id).Count(), Is.EqualTo(0));

            var a = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block a"));
            var b = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block b"));
            var cancel = new CancellationTokenSource();
            var task = bitSwapService.WantAsync(a.Id, _self.Id, cancel.Token);
            Assert.That(task.IsCompleted, Is.False);
            bitSwapService.PeerWants(_self.Id).ToArray().Should().Contain(a.Id);

            bitSwapService.Found(b);
            Assert.That(task.IsCompleted, Is.False);
            bitSwapService.PeerWants(_self.Id).ToArray().Should().Contain(a.Id);

            bitSwapService.Found(a);
            Assert.That(task.IsCompleted, Is.True);
            bitSwapService.PeerWants(_self.Id).ToArray().Should().NotContain(a.Id);
            a.DataBytes.Should().Contain(task.Result.DataBytes);
        }

        [Test]
        public void Found_Count()
        {
            var bitSwapService = new BitSwapService(new SwarmService(_self));

            var a = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block a"));
            Assert.That(bitSwapService.Found(a), Is.EqualTo(0));

            var cancel = new CancellationTokenSource();
            var task1 = bitSwapService.WantAsync(a.Id, _self.Id, cancel.Token);
            var task2 = bitSwapService.WantAsync(a.Id, _self.Id, cancel.Token);
            Assert.That(bitSwapService.Found(a), Is.EqualTo(2));

            Assert.That(task1.IsCompleted, Is.True);
            Assert.That(task2.IsCompleted, Is.True);
        }
    }
}
