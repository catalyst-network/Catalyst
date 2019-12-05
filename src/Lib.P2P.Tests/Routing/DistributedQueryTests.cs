using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests.Routing
{
    [TestClass]
    public class DistributedQueryTest
    {
        [TestMethod]
        public async Task Cancelling()
        {
            var dquery = new DistributedQuery<Peer>
            {
                Dht = new Dht1()
            };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            await dquery.RunAsync(cts.Token);
            Assert.AreEqual(0, dquery.Answers.Count());
        }

        [TestMethod]
        public void UniqueId()
        {
            var q1 = new DistributedQuery<Peer>();
            var q2 = new DistributedQuery<Peer>();
            Assert.AreNotEqual(q1.Id, q2.Id);
        }
    }
}
