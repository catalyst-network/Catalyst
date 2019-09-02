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
using Catalyst.Abstractions.Mempool.Documents;
using Catalyst.Abstractions.Mempool.Repositories;
using Catalyst.Core.Mempool.Documents;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Mempool
{
    public sealed class MempoolTests
    {
        private readonly Core.Mempool.Mempool _memPool;
        private readonly TransactionBroadcast _transactionBroadcast;
        private readonly ILogger _logger;

        public MempoolTests()
        {
            _logger = Substitute.For<ILogger>();
            _memPool = new Core.Mempool.Mempool(Substitute.For<IMempoolRepository<MempoolDocument>>(), _logger);

            // _mempoolDocument = new MempoolDocument
            // {
            //     Transaction = TransactionHelper.GetTransaction()
            // };

            _transactionBroadcast = TransactionHelper.GetTransaction();
        }

        private void AddKeyValueStoreEntryExpectation(TransactionBroadcast transaction)
        {
            var mempoolDoc = new MempoolDocument
            {
                Transaction = transaction
            };
            
            _memPool.Repository.ReadItem(Arg.Is<TransactionSignature>(k => k.Equals(transaction.Signature)))
               .Returns(mempoolDoc);
            
            _memPool.Repository.TryReadItem(Arg.Is<TransactionSignature>(k => k.Equals(transaction.Signature)))
               .Returns(true);
        }

        [Fact]
        public void Get_should_retrieve_a_saved_transaction()
        {
            _memPool.Repository.CreateItem(_transactionBroadcast);
            AddKeyValueStoreEntryExpectation(_transactionBroadcast);

            var mempoolDocument = _memPool.Repository.ReadItem(_transactionBroadcast.Signature);
            var expectedTransaction = _transactionBroadcast;
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
            var documents = GetTestingMempoolDocuments(numTx);
            documents.ForEach(AddKeyValueStoreEntryExpectation);

            for (var i = 0; i < numTx; i++)
            {
                var signature = TransactionHelper.GetTransactionSignature(signature: $"key{i}");
                var mempoolDocument = _memPool.GetMempoolDocument(signature);
                mempoolDocument.Transaction.STEntries.Single().Amount.Should().Be((uint) i);
            }
        }

        [Fact]
        public void Delete_should_delete_all_transactions()
        {
            var keys = Enumerable.Range(0, 10).Select(i => i.ToString()).ToArray();
            _memPool.Repository.DeleteItem(keys);
            _memPool.Repository.Received(1).DeleteItem(Arg.Is<string[]>(s => s.SequenceEqual(keys)));
        }

        [Fact]
        public void Delete_should_log_deletion_errors()
        {
            var keys = Enumerable.Range(0, 3).Select(i => i.ToString()).ToArray();
            var connectTimeoutException = new TimeoutException("that mempool connection was too slow");  
            _memPool.Repository.WhenForAnyArgs(t => t.DeleteItem(keys))
               .Throw(connectTimeoutException);

            _memPool.Delete(keys);

            _logger.Received(1).Error(Arg.Any<Exception>(),
                Arg.Any<string>(),
                Arg.Any<string[]>());
        }

        [Fact]
        public void Get_When_Key_Not_In_Store_Should_Throw()
        {
            _memPool.Repository.ReadItem(Arg.Any<TransactionSignature>()).ThrowsForAnyArgs(new KeyNotFoundException());
            new Action(() => _memPool.GetMempoolDocument(TransactionHelper.GetTransactionSignature("signature that doesn't exist")))
               .Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void SaveMempoolDocument_Should_Not_Override_Existing_Record()
        {
            var expectedAmount = _transactionBroadcast.STEntries.Single().Amount;

            var saved = _memPool.SaveMempoolDocument(_transactionBroadcast);
            AddKeyValueStoreEntryExpectation(_transactionBroadcast);
            saved.Should().BeTrue();

            var overridingTransaction = _transactionBroadcast.Clone();
            overridingTransaction.STEntries.Single().Amount = expectedAmount + 100;
            var overriden = _memPool.SaveMempoolDocument(overridingTransaction);

            overriden.Should().BeFalse();

            var retrievedTransaction = _memPool.GetMempoolDocument(_mempoolDocument.Transaction.Signature);
            retrievedTransaction.Transaction.STEntries.Single().Amount.Should().Be(expectedAmount);
        }

        [Fact]
        public void SaveMempoolDocument_Should_Return_False_And_Log_On_Store_Exception()
        {
            var exception = new TimeoutException("underlying store is not connected");
            _transactionStore.TryGet(default, out Arg.Any<MempoolDocument>())
               .ThrowsForAnyArgs(exception);
            var saved = _memPool.SaveMempoolDocument(_mempoolDocument);

            saved.Should().BeFalse();

            _logger.Received(1).Error(exception, Arg.Any<string>());
        }
        
        [Fact]
        public void SaveMempoolDocument_Should_Throw_On_Document_With_Null_Transaction()
        {
            _mempoolDocument.Transaction = null;
            new Action(() => _memPool.SaveMempoolDocument(_mempoolDocument))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null");
        }

        [Fact]
        public void SaveMempoolDocument_Should_Throw_On_Null_Document()
        {
            new Action(() => _memPool.SaveMempoolDocument(null))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null"); // transaction is null so do not insert
        }

        [Fact]
        public void GetMempoolContent_should_return_all_documents_from_mempool()
        {
            var documentCount = 13;
            var mempoolDocs = GetTestingMempoolDocuments(documentCount);

            _transactionStore.Repository.GetAll().Returns(mempoolDocs);

            var content = _memPool.GetMemPoolContent().ToList();

            _transactionStore.ReceivedWithAnyArgs(1).GetAll();

            content.Count.Should().Be(documentCount);
            content.Select(d => d.ToByteString()).Should()
               .BeEquivalentTo(mempoolDocs.Select(d => d.Transaction.ToByteString()));
        }

        private static List<MempoolDocument> GetTestingMempoolDocuments(int documentCount)
        {
            return Enumerable.Range(0, documentCount).Select(i =>
                    new MempoolDocument {Transaction = TransactionHelper.GetTransaction((uint) i, signature: $"key{i.ToString()}")})
               .ToList();
        }

        [Fact]
        public void GetMempoolContentEncoded_should_return_an_array_of_bytes_of_strings_of_all_transactions()
        {
            var documentCount = 7;
            var mempoolDocs = GetTestingMempoolDocuments(documentCount);

            _transactionStore.GetAll().Returns(mempoolDocs);

            var content = _memPool.GetMemPoolContentAsTransactions().ToList();

            _transactionStore.ReceivedWithAnyArgs(1).GetAll();

            content.Count.Should().Be(documentCount);
            content.Select(d => d.ToByteString()).Should()
               .BeEquivalentTo(mempoolDocs.Select(d => d.Transaction.ToByteString()));
        }

        [Fact]
        public void ContainsDocument_Should_Return_True_On_Known_DocumentId()
        {
            AddKeyValueStoreEntryExpectation(_mempoolDocument);
            _memPool.ContainsDocument(_mempoolDocument.Transaction.Signature).Should().BeTrue();
        }

        [Fact]
        public void ContainsDocument_Should_Return_False_On_Unknown_DocumentId()
        {
            var unknownTransaction = TransactionHelper.GetTransactionSignature("key not in the mempool");
            _memPool.ContainsDocument(unknownTransaction).Should().BeFalse();
        }
    }
}
