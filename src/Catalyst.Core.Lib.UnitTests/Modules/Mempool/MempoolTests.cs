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
using System.Linq.Expressions;
using System.Threading;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.Modules.Mempool.Models;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.Modules.Mempool
{
    public sealed class MempoolTests
    {
        public MempoolTests()
        {
            _transactionStore = Substitute.For<IMempoolRepository>();
            var logger = Substitute.For<ILogger>();
            _memPool = new Catalyst.Core.Lib.Modules.Mempool.Mempool(_transactionStore, logger);

            _mempoolDocument = new MempoolDocument
            {
                Transaction = TransactionHelper.GetTransaction()
            };
        }

        private readonly Catalyst.Core.Lib.Modules.Mempool.Mempool _memPool;

        private readonly IMempoolRepository _transactionStore;
        private readonly IMempoolDocument _mempoolDocument;

        private static void AddKeyValueStoreEntryExpectation(IMempoolDocument document,
            IMempoolRepository store)
        {
            store.Get(Arg.Is<string>(k => k.Equals(document.DocumentId)))
               .Returns(document);
            store.TryGet(Arg.Is<string>(k => k.Equals(document.Transaction.Signature)),
                    out Arg.Any<MempoolDocument>())
               .Returns(ci =>
                {
                    ci[1] = document.Transaction;
                    return true;
                });
        }

        private sealed class ProducerConsumer
        {
            private static readonly object Locker = new object();
            private readonly IMempoolRepository _keyValueStore;
            private readonly IMempool _memPool;
            public int? FirstThreadId;

            public ProducerConsumer(IMempool memPool, IMempoolRepository keyValueStore)
            {
                _memPool = memPool;
                _keyValueStore = keyValueStore;
            }

            public void Writer()
            {
                IMempoolDocument mempoolDocument;
                lock (Locker)
                {
                    var id = Thread.CurrentThread.ManagedThreadId;
                    mempoolDocument = new MempoolDocument
                    {
                        Transaction = TransactionHelper.GetTransaction(standardAmount: (uint) id)
                    };

                    if (FirstThreadId == null)
                    {
                        FirstThreadId = id;
                        AddKeyValueStoreEntryExpectation(mempoolDocument, _keyValueStore);
                    }

                    Thread.Sleep(5); //milliseconds
                }

                // here there could be a context switch so second thread might call SaveTransaction failing the test
                // so a little sleep was needed in the locked section
                _memPool.SaveMempoolDocument(mempoolDocument); // write same key but different tx amount, not under lock
            }
        }

        [Fact]
        public void Get_should_retrieve_a_saved_transaction()
        {
            _memPool.SaveMempoolDocument(_mempoolDocument);
            AddKeyValueStoreEntryExpectation(_mempoolDocument, _transactionStore);

            var mempoolDocument = _memPool.GetMempoolDocument(_mempoolDocument.Transaction.Signature);
            var expectedTransaction = _mempoolDocument.Transaction;
            var transactionFromMemPool = mempoolDocument.Transaction;

            transactionFromMemPool.STEntries.Single().Amount.Should().Be(expectedTransaction.STEntries.Single().Amount);
            transactionFromMemPool.CFEntries.Single().PedersenCommit.Should().BeEquivalentTo(expectedTransaction.CFEntries.Single().PedersenCommit);
            transactionFromMemPool.Signature.Should().Be(expectedTransaction.Signature);
            transactionFromMemPool.Version.Should().Be(expectedTransaction.Version);
            transactionFromMemPool.LockTime.Should().Be(expectedTransaction.LockTime);
            transactionFromMemPool.TimeStamp.Should().Be(expectedTransaction.TimeStamp);
            transactionFromMemPool.TransactionFees.Should().Be(expectedTransaction.TransactionFees);
        }

        [Fact]
        public void Get_should_retrieve_saved_transaction_matching_their_keys()
        {
            const int numTx = 10;
            for (var i = 0; i < numTx; i++)
            {
                var mempoolDocument =
                    new MempoolDocument {Transaction = TransactionHelper.GetTransaction(standardAmount: (uint) i, signature: $"key{i}")};
                _memPool.SaveMempoolDocument(mempoolDocument);
                AddKeyValueStoreEntryExpectation(mempoolDocument, _transactionStore);
            }

            for (var i = 0; i < numTx; i++)
            {
                var signature = TransactionHelper.GetTransactionSignature(signature: $"key{i}");
                var mempoolDocument = _memPool.GetMempoolDocument(signature);
                mempoolDocument.Transaction.STEntries.Single().Amount.Should().Be((uint) i);
            }
        }

        [Fact]
        public void Clear_should_delete_all_transactions()
        {
            var keys = Enumerable.Range(0, 10).Select(i => TransactionHelper.GetTransactionSignature($"{i}"));
            _memPool.Delete(keys.ToArray());
            _transactionStore.Received(10).Delete(Arg.Any<Expression<Func<MempoolDocument, bool>>>());
        }

        [Fact]
        public void GetNonExistentKey()
        {
            _transactionStore.Get(Arg.Any<string>()).ThrowsForAnyArgs(new KeyNotFoundException());
            new Action(() => _memPool.GetMempoolDocument(TransactionHelper.GetTransactionSignature("signature that doesn't exist")))
               .Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void KeyAlreadyExists()
        {
            var expectedAmount = _mempoolDocument.Transaction.STEntries.Single().Amount;
            _memPool.SaveMempoolDocument(_mempoolDocument);
            AddKeyValueStoreEntryExpectation(_mempoolDocument, _transactionStore);

            var overridingTransaction = new MempoolDocument {Transaction = _mempoolDocument.Transaction.Clone()};
            overridingTransaction.Transaction.STEntries.Single().Amount = expectedAmount + 100;
            _memPool.SaveMempoolDocument(overridingTransaction);

            var retrievedTransaction = _memPool.GetMempoolDocument(_mempoolDocument.Transaction.Signature);
            retrievedTransaction.Transaction.STEntries.Single().Amount.Should().Be(expectedAmount);
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

            var mempoolDocument = _memPool.GetMempoolDocument(_mempoolDocument.Transaction.Signature);

            // the first thread should set the amount and the value not overridden by other threads
            // trying to insert the same key
            ((int) mempoolDocument.Transaction.STEntries.Single().Amount).Should().Be(pc.FirstThreadId);
        }

        [Fact]
        public void SaveNullKey()
        {
            _mempoolDocument.Transaction.Signature = null;
            new Action(() => _memPool.SaveMempoolDocument(_mempoolDocument))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null");
        }

        [Fact]
        public void SaveNullTx()
        {
            new Action(() => _memPool.SaveMempoolDocument(null))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null"); // transaction is null so do not insert
        }
    }
}
