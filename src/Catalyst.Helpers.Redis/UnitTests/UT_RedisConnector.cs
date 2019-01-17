using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using Catalyst.Redis;

namespace Catalyst.Helpers.Redis.UnitTests
{
    [TestClass]
    public class UT_RedisConnector
    {
        private static readonly ConnectionMultiplexer Cm = RedisConnector.Instance().Connection;
        private static readonly IDatabase Db = RedisConnector.Instance().GetDb;

        private static void Writer(int start, int end)
        {
            for (var k = start; k < end; k++)
            {
                Assert.IsTrue(Db.StringSet($"mykey:{k}", k));
            }            
        }

        private static void Reader(int start, int end)
        {
            for (var k = start; k < end; k++)
            {
                Assert.AreEqual(k, Db.StringGet($"mykey:{k}"));
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            var endpoint = Cm.GetEndPoints();
            Assert.AreEqual(1,endpoint.Length);

            var server = Cm.GetServer(endpoint[0]);
            server.FlushDatabase(); // clean up before each test
        }

        [TestMethod]
        public void OneWriteRead()
        {
            Assert.IsTrue(Db.StringSet("firstkey", 100));
            Assert.AreEqual(100, Db.StringGet("firstkey"));
        }
        
        [TestMethod]
        public void WriteByteArrayValue()
        {
            var bytes = Encoding.ASCII.GetBytes("abcdef");
            
            Assert.IsTrue(Db.StringSet("firstkey",bytes));
            Assert.AreEqual("abcdef", Encoding.ASCII.GetString(Db.StringGet("firstkey")));
        }

        [TestMethod]
        public void KeyDoesNotExists()
        {
            var ret = Db.StringGet("mykey");
            if (ret.HasValue)
            {
                Assert.Fail("Expected no value with this key in Redis");
            }
        }

        [TestMethod]
        public void KeyAlreadyExistUpdate()
        {
            Assert.IsTrue(Db.StringSet("mykey", 100));
            Assert.AreEqual(100, Db.StringGet("mykey"));
            Assert.IsTrue(Db.StringSet("mykey",200));
            Assert.AreEqual(200, Db.StringGet("mykey"));
        }
        
        [TestMethod]
        public void WriteReadMany()
        {            
            const int num = 15000;
            for (var i = 0; i < num; i++)
            {
                Db.StringSet($"mykey:{i}",i);
            }
            
            for (var i = 0; i < num; i++)
            {
                Assert.AreEqual(i, Db.StringGet($"mykey:{i}"));
            }
        }

        [TestMethod]
        public void MultipleClient()
        {
            const int threadNum = 5;
            var threadW = new Thread[threadNum];
            var threadR = new Thread[threadNum];

            // Set up writer and reader
            for (var i = 0; i < threadNum; i++)
            {
                var i1 = i;
                threadW[i] = new Thread(() => Writer((i1 * 10000), (i1 + 1) * 10000));
                threadR[i] = new Thread(() => Reader((i1 * 10000), (i1 + 1) * 10000));
            }

            // Writers need to put stuff in the DB first
            for (var i = 0; i < threadNum; i++)
            {
                threadW[i].Start();
            }

            for (var i = 0; i < threadNum; i++)
            {
                threadW[i].Join();
            }

            // ... and now multiple readers
            for (var i = 0; i < threadNum; i++)
            {
                threadR[i].Start();
            }

            for (var i = 0; i < threadNum; i++)
            {
                threadR[i].Join();
            }
        }
    }
}
