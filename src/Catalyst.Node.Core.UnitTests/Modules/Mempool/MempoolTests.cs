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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.Modules.Mempool
{
    public sealed class MempoolTests
    {
        public MempoolTests()
        {
            _transactionStore = Substitute.For<IRepository<TransactionBroadcast, TransactionSignature>>();
            var logger = Substitute.For<ILogger>();
            _memPool = new Core.Modules.Mempool.Mempool(_transactionStore, logger);

            _transaction = TransactionHelper.GetTransaction();
        }

        private readonly Core.Modules.Mempool.Mempool _memPool;

        private readonly IRepository<TransactionBroadcast, TransactionSignature> _transactionStore;
        private readonly TransactionBroadcast _transaction;

        private static void AddKeyValueStoreEntryExpectation(TransactionBroadcast transaction,
            IRepository<TransactionBroadcast, TransactionSignature> store)
        {
            store.Get(Arg.Is<TransactionSignature>(k => k.Equals(transaction.Signature)))
               .Returns(transaction);
            store.TryGet(Arg.Is<TransactionSignature>(k => k.Equals(transaction.Signature)),
                    out Arg.Any<TransactionBroadcast>())
               .Returns(ci =>
                {
                    ci[1] = transaction;
                    return true;
                });
        }

        private sealed class ProducerConsumer
        {
            private static readonly object Locker = new object();
            private readonly IRepository<TransactionBroadcast, TransactionSignature> _keyValueStore;
            private readonly IMempool _memPool;
            public int? FirstThreadId;

            public ProducerConsumer(IMempool memPool, IRepository<TransactionBroadcast, TransactionSignature> keyValueStore)
            {
                _memPool = memPool;
                _keyValueStore = keyValueStore;
            }

            public void Writer()
            {
                TransactionBroadcast transaction;
                lock (Locker)
                {
                    var id = Thread.CurrentThread.ManagedThreadId;
                    transaction = TransactionHelper.GetTransaction(standardAmount: (uint) id);

                    if (FirstThreadId == null)
                    {
                        FirstThreadId = id;
                        AddKeyValueStoreEntryExpectation(transaction, _keyValueStore);
                    }

                    Thread.Sleep(5); //milliseconds
                }

                // here there could be a context switch so second thread might call SaveTransaction failing the test
                // so a little sleep was needed in the locked section
                _memPool.SaveTransaction(transaction); // write same key but different tx amount, not under lock
            }
        }

        [Fact]
        public void Get_should_retrieve_a_saved_transaction()
        {
            _memPool.SaveTransaction(_transaction);
            AddKeyValueStoreEntryExpectation(_transaction, _transactionStore);

            var transactionFromMemPool = _memPool.GetTransaction(_transaction.Signature);

            transactionFromMemPool.STEntries.Single().Amount.Should().Be(_transaction.STEntries.Single().Amount);
            transactionFromMemPool.CFEntries.Single().PedersenCommit.Should().BeEquivalentTo(_transaction.CFEntries.Single().PedersenCommit);
            transactionFromMemPool.Signature.Should().Be(_transaction.Signature);
            transactionFromMemPool.Version.Should().Be(_transaction.Version);
            transactionFromMemPool.LockTime.Should().Be(_transaction.LockTime);
            transactionFromMemPool.TimeStamp.Should().Be(_transaction.TimeStamp);
            transactionFromMemPool.TransactionFees.Should().Be(_transaction.TransactionFees);
        }

        [Fact]
        public void Get_should_retrieve_saved_transaction_matching_their_keys()
        {
            const int numTx = 10;
            for (var i = 0; i < numTx; i++)
            {
                var transaction = TransactionHelper.GetTransaction(standardAmount: (uint) i, signature: $"key{i}");
                _memPool.SaveTransaction(transaction);
                AddKeyValueStoreEntryExpectation(transaction, _transactionStore);
            }

            for (var i = 0; i < numTx; i++)
            {
                var signature = TransactionHelper.GetTransactionSignature(signature: $"key{i}");
                var transaction = _memPool.GetTransaction(signature);
                transaction.STEntries.Single().Amount.Should().Be((uint) i);
            }
        }

        [Fact]
        public void GetNonExistentKey()
        {
            _transactionStore.Get(Arg.Any<TransactionSignature>()).ThrowsForAnyArgs(new KeyNotFoundException());
            new Action(() => _memPool.GetTransaction(TransactionHelper.GetTransactionSignature("signature that doesn't exist")))
               .Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void KeyAlreadyExists()
        {
            var expectedAmount = _transaction.STEntries.Single().Amount;
            _memPool.SaveTransaction(_transaction);
            AddKeyValueStoreEntryExpectation(_transaction, _transactionStore);

            var overridingTransaction = _transaction.Clone();
            overridingTransaction.STEntries.Single().Amount = expectedAmount + 100;
            _memPool.SaveTransaction(overridingTransaction);

            var retrievedTransaction = _memPool.GetTransaction(_transaction.Signature);
            retrievedTransaction.STEntries.Single().Amount.Should().Be(expectedAmount);
        }

        [Fact]
        public void MultipleThreadsSameKey()
        {
            const int threadNum = 8;
            var threadW = new Thread[threadNum];
            var pc = new ProducerConsumer(_memPool, _transactionStore);

            // Set up writer
            for (var i = 0; i < threadNum; i++) threadW[i] = new Thread(pc.Writer);

            // Writers need to put stuff in the DB first
            for (var i = 0; i < threadNum; i++) threadW[i].Start();

            for (var i = 0; i < threadNum; i++) threadW[i].Join();

            var transaction = _memPool.GetTransaction(_transaction.Signature);

            // the first thread should set the amount and the value not overridden by other threads
            // trying to insert the same key
            ((int) transaction.STEntries.Single().Amount).Should().Be(pc.FirstThreadId);
        }

        [Fact]
        public void SaveNullKey()
        {
            _transaction.Signature = null;
            new Action(() => _memPool.SaveTransaction(_transaction))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null");
        }

        [Fact]
        public void SaveNullTx()
        {
            new Action(() => _memPool.SaveTransaction(null))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null"); // transaction is null so do not insert
        }
    }
}
