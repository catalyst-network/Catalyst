using StackExchange.Redis;
using ADL.Utilities;
using ADL.Mempool.Proto;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADL.Mempool
{
    [TestClass]
    public class UT_Mempool
    {
        private static readonly ConnectionMultiplexer Cm = RedisConnector.Instance.Connection;
        private static MempoolService Memp = new MempoolService();
        
        private readonly Key _k = new Key();
        private readonly Tx _t = new Tx();
        
        [TestInitialize]
        public void Initialize()
        {
            _k.HashedSignature = "hashed_signature";

            _t.Amount = 0.5;
            _t.Signature = "signature";
            _t.AddressDest = "address_dest";
            _t.AddressSource = "address_source"; 
            _t.Updated = new Timestamp {Nanos = 100, Seconds = 30};
            
            var endpoint = Cm.GetEndPoints();
            Assert.AreEqual(1,endpoint.Length);
            
            var server = Cm.GetServer(endpoint[0]);
            server.FlushDatabase(); // clean up Redis before each test
        }
        
        [TestMethod]
        public void SaveAndGet()
        {   
            Memp.Save(_k,_t);
            var transaction = Memp.Get(_k);
            
            Assert.AreEqual(0.5, transaction.Amount);
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
                Memp.Save(new Key {HashedSignature = $"just_a_short_key_for_easy_search:{i}"}, _t);
            }
            
            for (var i = 0; i < numTx; i++)
            {
                var transaction = Memp.Get(new Key{ HashedSignature = $"just_a_short_key_for_easy_search:{i}"});
                Assert.AreEqual(0.5, transaction.Amount);
                Assert.AreEqual("signature", transaction.Signature);
                Assert.AreEqual("address_dest", transaction.AddressDest);
                Assert.AreEqual("address_source", transaction.AddressSource);
                Assert.AreEqual(100, transaction.Updated.Nanos);
                Assert.AreEqual(30, transaction.Updated.Seconds);
            }
        }
    }
}