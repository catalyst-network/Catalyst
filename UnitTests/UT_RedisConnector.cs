using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace ADL.Utilities
{
    [TestClass]
    public class UT_RedisConnector
    {
        private static readonly ConnectionMultiplexer Cm = RedisConnector.Instance.Connection;
        private static readonly IDatabase Db = Cm.GetDatabase();

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
    }
}