using System;
using System.Diagnostics;
using System.Threading;
using StackExchange.Redis;
using ADL.Bash;
using ADL.Node.Core.Modules.Mempool;
using ADL.Protocol.Mempool.dist.csharp;
using ADL.Redis;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADL.UnitTests
{
    [TestClass]
    public class UT_Mempool
    {
        private static readonly ConnectionMultiplexer Cm = RedisConnector.Instance.Connection;
        private static Mempool Memp = new Mempool(new Redis.Redis());
        
        private static Key _k = new Key();
        private static Tx _t = new Tx();

        private class ProducerConsumer
        {
            static readonly object locker = new object();
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
                }

                Memp.SaveTx(_k, _t); // write same key but different tx amount, not under lock                
            }
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
            //server.FlushAllDatabases();
            
            server.ConfigSet("save","1 1"); // save every seconds for each change to the dataset
        }
        
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
            Thread.Sleep(1100); // after one second the changes is saved
            
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