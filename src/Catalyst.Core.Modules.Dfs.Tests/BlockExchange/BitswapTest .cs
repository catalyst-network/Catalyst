using System.Linq;
using System.Text;
using System.Threading;
using Catalyst.Core.Modules.Dfs.BlockExchange;
using FluentAssertions;
using Lib.P2P;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.BlockExchange
{
    public class BitswapTest
    {
        Peer self = new Peer
        {
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey =
                "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        [Fact]
        public void WantList()
        {
            var bitswap = new Bitswap {Swarm = new Swarm {LocalPeer = self}};
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
            var bitswap = new Bitswap {Swarm = new Swarm {LocalPeer = self}};
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
            var bitswap = new Bitswap {Swarm = new Swarm {LocalPeer = self}};
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
            var bitswap = new Bitswap {Swarm = new Swarm {LocalPeer = self}};
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
            var bitswap = new Bitswap {Swarm = new Swarm {LocalPeer = self}};
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
            var bitswap = new Bitswap {Swarm = new Swarm {LocalPeer = self}};

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
