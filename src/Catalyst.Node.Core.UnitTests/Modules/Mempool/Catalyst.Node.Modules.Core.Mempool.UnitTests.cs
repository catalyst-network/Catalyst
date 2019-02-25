using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Modules.Mempool;
using Catalyst.Node.Core.Modules.Mempool;
using Catalyst.Protocols.Transaction;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.UnitTests.Modules.Mempool
{
    public class UT_Mempool
    {
        private readonly Core.Modules.Mempool.Mempool _memPool;

        private readonly IRepository<StTxModel> _keyValueStore;
        private readonly Key _key;
        private readonly StTx _transaction;
        private ILogger _logger;

        public UT_Mempool()
        {
            _keyValueStore = Substitute.For<IRepository<StTxModel>>();
            _logger = Substitute.For<ILogger>();
            _memPool = new Core.Modules.Mempool.Mempool(_keyValueStore, _logger);

            _key = new Key {HashedSignature = "hashed_signature"};
            _transaction = new StTx
                           {
                               Amount = 1,
                               Signature = "signature",
                               AddressDest = "address_dest",
                               AddressSource = "address_source",
                               Updated = new StTx.Types.Timestamp {Nanos = 100, Seconds = 30}
                           };
        }

        private static void AddKeyValueStoreEntryExpectation(Key key, StTx tx, IRepository<StTxModel> keyValueStore)
        {
            var record = new StTxModel() {Key = key, Transaction = tx};
            keyValueStore.TryFind(Arg.Is<Expression<Func<StTxModel, bool>>>(e => 
            e.Key.ToByteArray().Equals(key.ToByteArray())), out Arg.Any<StTxModel>())
                .Returns(a =>
                {
                    a[1] = tx;
                    return true;
                });
        }

        private class ProducerConsumer
        {
            private static readonly object Locker = new object();
            private readonly Key _key;
            private readonly IRepository<StTxModel> _keyValueStore;
            private readonly IMempool _memPool;
            public int? FirstThreadId;

            public ProducerConsumer(IMempool memPool, Key key, IRepository<StTxModel> keyValueStore)
            {
                _memPool = memPool;
                _key = key;
                _keyValueStore = keyValueStore;
            }

            public void Writer()
            {
                StTx transaction;
                lock (Locker)
                {
                    var id = Thread.CurrentThread.ManagedThreadId;
                    transaction = new StTx {Amount = (uint) id};

                    if (FirstThreadId == null)
                    {
                        FirstThreadId = id;
                        AddKeyValueStoreEntryExpectation(_key, transaction, _keyValueStore);
                    }

                    Thread.Sleep(5); //milliseconds
                }

                // here there could be a context switch so second thread might call SaveTx failing the test
                // so a little sleep was needed in the locked section
                _memPool.SaveTx(_key, transaction); // write same key but different tx amount, not under lock
            }
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

        [Fact]
        public void Get_should_retrieve_a_saved_transaction()
        {
            _memPool.SaveTx(_key, _transaction);
            AddKeyValueStoreEntryExpectation(_key, _transaction, _keyValueStore);

            var transactionFromMemPool = _memPool.GetTx(_key);

            transactionFromMemPool.Amount.Should().Be(_transaction.Amount);
            transactionFromMemPool.Signature.Should().Be(_transaction.Signature);
            transactionFromMemPool.AddressDest.Should().Be(_transaction.AddressDest);
            transactionFromMemPool.AddressSource.Should().Be(_transaction.AddressSource);
            transactionFromMemPool.Updated.Nanos.Should().Be(_transaction.Updated.Nanos);
            transactionFromMemPool.Updated.Seconds.Should().Be(_transaction.Updated.Seconds);
        }

        [Fact]
        public void Get_should_retrieve_saved_transaction_matching_their_keys()
        {
            const int numTx = 10;
            for (var i = 0; i < numTx; i++)
            {
                var key = new Key {HashedSignature = $"just_a_short_key_for_easy_search:{i}"};
                var transaction = new StTx {Amount = (uint) i};
                _memPool.SaveTx(key, transaction);
                AddKeyValueStoreEntryExpectation(key, transaction, _keyValueStore);
            }

            for (var i = 0; i < numTx; i++)
            {
                var key = new Key {HashedSignature = $"just_a_short_key_for_easy_search:{i}"};
                var transaction = _memPool.GetTx(key);
                transaction.Amount.Should().Be((uint) i);
            }
        }

        [Fact]
        public void GetNonExistentKey()
        {
            _keyValueStore.TryFind(Arg.Any<Expression<Func<StTxModel, bool>>>(), out Arg.Any<StTxModel>())
               .ThrowsForAnyArgs(new KeyNotFoundException());
            new Action(() => _memPool.GetTx(new Key {HashedSignature = "just_a_short_key_for_easy_search:0"}))
               .Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void KeyAlreadyExists()
        {
            var key = new Key {HashedSignature = "just_a_short_key_for_easy_search:0"};

            _memPool.SaveTx(key, _transaction);
            //This is really testing the underlying IKeyValueStore behavior...
            var tx = _transaction;
            AddKeyValueStoreEntryExpectation(key, tx, _keyValueStore);

            _transaction.Amount = 100;
            _memPool.SaveTx(key, _transaction);

            var transaction = _memPool.GetTx(key);
            transaction.Amount.Should().Be(1); // assert tx with same key not updated
        }

        [Fact]
        public void MultipleThreadsSameKey()
        {
            const int threadNum = 8;
            var threadW = new Thread[threadNum];
            var pc = new ProducerConsumer(_memPool, _key, _keyValueStore);

            // Set up writer
            for (var i = 0; i < threadNum; i++) threadW[i] = new Thread(pc.Writer);

            // Writers need to put stuff in the DB first
            for (var i = 0; i < threadNum; i++) threadW[i].Start();

            for (var i = 0; i < threadNum; i++) threadW[i].Join();

            var transaction = _memPool.GetTx(_key);

            // the first thread should set the amount and the value not overridden by other threads
            // trying to insert the same key
            ((int) transaction.Amount).Should().Be(pc.FirstThreadId);
        }

        [Fact]
        public void SaveNullKey()
        {
            new Action(() => _memPool.SaveTx(null, _transaction))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null");
        }

        [Fact]
        public void SaveNullTx()
        {
            var newKey = new Key {HashedSignature = "just_a_short_key_for_easy_search:0"};

            new Action(() => _memPool.SaveTx(newKey, null))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null"); // transaction is null so do not insert
        }
    }
}