using System;
using System.Net;
using System.Threading;
using System.Diagnostics;
using StackExchange.Redis;
using Catalyst.Helpers.Bash;
using Catalyst.Helpers.Redis;
using Catalyst.Protocols.Mempool;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catalyst.Node.Modules.Core.Mempool.UnitTests
{
    [TestClass]
    public class UT_Mempool
    {
        private static readonly ConnectionMultiplexer Cm = RedisConnector.GetInstance(Helpers.Network.EndpointBuilder.BuildNewEndPoint("127.0.0.1", 6379)).Connection;
        
        private class TestMempoolSettings : IMempoolSettings // sort of mock
        {
            public IPEndPoint Host { get; set; }
            public string Type { get; set; }
            public string Expiry {get; set; }
            public string When { get; set; }
        }

        private static TestMempoolSettings _settings;
        private static Mempool Memp = new Mempool(new Redis());
        
        private static Key _k = new Key();
        private static Tx _t = new Tx();
                
        private class ProducerConsumer
        {
            private static readonly object locker = new object();
            public object firstThreadId = null;
            
            public void Writer()
            {
                lock (locker)
                {
                    var id = Thread.CurrentThread.ManagedThreadId;
                    if (firstThreadId == null)
                    {
                        firstThreadId = id;
                    }

                    _t.Amount = (uint)id;
                    
                    Thread.Sleep(5); //milliseconds
                }
                
                // here there could be a context switch so second thread might call SaveTx failing the test
                // so a little sleep was needed in the locked section
                                
                Memp.SaveTx(_k, _t); // write same key but different tx amount, not under lock                
            }
        }
        
        [ClassInitialize]
        public static void SetUp(TestContext testContext)
        {
            _settings = new TestMempoolSettings {Type = "redis", When = "NotExists"};
        }
        
        [TestInitialize]
        public void Initialize()
        {
            _k.HashedSignature = "hashed_signature";

            _t.Amount = 1;
            _t.Signature = "signature";
            _t.AddressDest = "address_dest";
            _t.AddressSource = "address_source"; 
            _t.Updated = new Tx.Types.Timestamp{Nanos = 100, Seconds = 30};
            
            var endpoint = Cm.GetEndPoints();
            Assert.AreEqual(1,endpoint.Length);
            
            var server = Cm.GetServer(endpoint[0]);
            server.FlushDatabase(); // clean up Redis before each test
            server.FlushAllDatabases();
            
            server.ConfigSet("save","1 1"); // save every seconds for each change to the dataset
            Thread.Sleep(500); //give it half a seconds to make changes affective
        }
        
//        [TestMethod]//@TODO fix this
//        public void GetInfo()
//        {
//            Memp.SaveTx(_k,_t);
//            
//            var response = Memp.GetInfo();

//            Assert.IsNotNull(response);
//            Assert.IsTrue(response.Any());
//            Assert.AreEqual(response["tcp_port"], "6379");
//            Assert.IsTrue(int.Parse(response["used_memory"]) < 1000000);
//            Assert.AreEqual(response["db0"], "keys=1,expires=0,avg_ttl=0");            
//        }
        
        [TestMethod]
        public void SaveAndGet()
        {   
            Memp.SaveTx(_k,_t);
            var transaction = Memp.GetTx(_k);
            Assert.AreEqual((uint)1, transaction.Amount);
            Assert.AreEqual("signature", transaction.Signature);
            Assert.AreEqual("address_dest", transaction.AddressDest);
            Assert.AreEqual("address_source", transaction.AddressSource);
            Assert.AreEqual(100, transaction.Updated.Nanos);
            Assert.AreEqual(30, transaction.Updated.Seconds);
        }

        [TestMethod]
        public void SaveAndGetMany()
        {
            const int numTx = 15000;
            for (var i = 0; i < numTx; i++)
            {
                Memp.SaveTx(new Key {HashedSignature = $"just_a_short_key_for_easy_search:{i}"}, _t);
            }
            
            for (var i = 0; i < numTx; i++)
            {
                var transaction = Memp.GetTx(new Key{ HashedSignature = $"just_a_short_key_for_easy_search:{i}"});
                Assert.AreEqual((uint)1, transaction.Amount);
                Assert.AreEqual("signature", transaction.Signature);
                Assert.AreEqual("address_dest", transaction.AddressDest);
                Assert.AreEqual("address_source", transaction.AddressSource);
                Assert.AreEqual(100, transaction.Updated.Nanos);
                Assert.AreEqual(30, transaction.Updated.Seconds);
            }
        }

        [TestMethod]
        public void KeyAlreadyExists()
        {
            var key = new Key {HashedSignature = "just_a_short_key_for_easy_search:0"};
            
            Memp.SaveTx(key, _t);

            _t.Amount = 100;
            
            Memp.SaveTx(key, _t);
            
            var transaction = Memp.GetTx(key);
            Assert.AreEqual((uint)1, transaction.Amount); // assert tx with same key not updated
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "Value cannot be null")]
        public void SaveNullKey()
        {
            Key newKey = null;

            Memp.SaveTx(newKey, _t);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "Value cannot be null")]
        public void SaveNullTx()
        {
            var newKey = new Key {HashedSignature = "just_a_short_key_for_easy_search:0"};
            Tx newTx = null;
            
            Memp.SaveTx(newKey, newTx); // transaction is null so do not insert
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "Value cannot be null")]
        public void GetNonExistentKey()
        {
            Memp.GetTx(new Key {HashedSignature = "just_a_short_key_for_easy_search:0"});
        }

        [TestMethod]
        public void MultipleThreadsSameKey()
        {
            const int threadNum = 8;
            var threadW = new Thread[threadNum];
            var pc = new ProducerConsumer();

            // Set up writer
            for (var i = 0; i < threadNum; i++)
            {
                threadW[i] = new Thread(pc.Writer);
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
           
            var transaction = Memp.GetTx(_k);
            
            // the first thread should set the amount and the value not overridden by other threads
            // trying to insert the same key
            Assert.AreEqual(pc.firstThreadId,(int)transaction.Amount);
        }
        
        [TestMethod]
        public void Reconnect()
        {
            var localByName = Process.GetProcessesByName("redis-server");
            if (localByName.Length > 0)
            {
                localByName[0].Kill(); // kill daemon process
            }
            
            // redis-server is down
            Assert.AreEqual(0, Process.GetProcessesByName("redis-server").Length);

            try
            {
                Memp.SaveTx(_k, _t);

                Assert.Fail("It should have thrown an exception if server is down");
            }
            catch (Exception)
            {
                "redis-server".BackgroundCmd(); // restart
            }
            
            localByName = Process.GetProcessesByName("redis-server");
            Assert.IsTrue(localByName.Length > 0);
            
            try
            {
                Memp.SaveTx(_k, _t);
                var transaction = Memp.GetTx(_k);
                
                Assert.AreEqual("signature", transaction.Signature);
            }
            catch (Exception e)
            {
                Assert.Fail("Not expected exception. It should have reconnected automatically " + e);
            }
        }

        [TestMethod]
        public void KeysArePersistent()
        {
            Memp.SaveTx(_k, _t);            
            Thread.Sleep(2000); // after one second the changes is saved
            
            var transaction = Memp.GetTx(_k);
            Assert.AreEqual("signature", transaction.Signature);
                        
            var localByName = Process.GetProcessesByName("redis-server");
            if (localByName.Length > 0)
            {
                localByName[0].Kill(); // kill daemon process
            }
                          
            "redis-server".BackgroundCmd(); // restart
            localByName = Process.GetProcessesByName("redis-server");
            Assert.IsTrue(localByName.Length > 0);

            transaction = Memp.GetTx(_k);
            Assert.AreEqual((uint)1, transaction.Amount);
        }
    }
}