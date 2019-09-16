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
using Catalyst.Abstractions.Mempool.Repositories;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Extensions.Protocol.Wire;
using Catalyst.Core.Lib.Mempool.Documents;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Nethermind.Dirichlet.Numerics;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Core.Modules.Mempool.Tests.UnitTests
{
    public sealed class MempoolTests
    {
        private readonly Mempool _memPool;

        private readonly TransactionBroadcast _transactionBroadcast;

        public MempoolTests()
        {
            var logger = Substitute.For<ILogger>();
            _memPool = new Mempool(Substitute.For<IMempoolRepository<MempoolDocument>>(), logger);

            _transactionBroadcast = TransactionHelper.GetPublicTransaction();
        }

        private void AddKeyValueStoreEntryExpectation(TransactionBroadcast transaction)
        {
            var mempoolDoc = new MempoolDocument
            {
                Transaction = transaction
            };
            
            _memPool.Repository.ReadItem(Arg.Is<ByteString>(k => k.SequenceEqual(transaction.Signature.RawBytes)))
               .Returns(mempoolDoc);
            
            _memPool.Repository.TryReadItem(Arg.Is<ByteString>(k => k.SequenceEqual(transaction.Signature.RawBytes)))
               .Returns(true);
        }

        [Fact]
        public void Get_should_retrieve_a_saved_transaction()
        {
            _memPool.Repository.CreateItem(_transactionBroadcast);
            AddKeyValueStoreEntryExpectation(_transactionBroadcast);

            var mempoolDocument = _memPool.Repository.ReadItem(_transactionBroadcast.Signature.RawBytes);
            var expectedTransaction = _transactionBroadcast;
            var transactionFromMemPool = mempoolDocument.Transaction;

            transactionFromMemPool.PublicEntries.Single().Amount.ToUInt256().Should().Be(expectedTransaction.PublicEntries.Single().Amount.ToUInt256());
            transactionFromMemPool.Signature.RawBytes.SequenceEqual(expectedTransaction.Signature.RawBytes).Should().BeTrue();
            transactionFromMemPool.Timestamp.Should().Be(expectedTransaction.Timestamp);
            transactionFromMemPool.SummedEntryFees().Should().Be(expectedTransaction.SummedEntryFees());
        }

        [Fact]
        public void Get_should_retrieve_saved_transaction_matching_their_keys()
        {
            const int numTx = 10;
            var documents = GetTestingMempoolDocuments(numTx);
            documents.ForEach(AddKeyValueStoreEntryExpectation);

            for (var i = 0; i < numTx; i++)
            {
                var signature = $"key{i}".ToUtf8ByteString();
                var mempoolDocument = _memPool.Repository.ReadItem(signature);
                mempoolDocument.Transaction.PublicEntries.Single().Amount.ToUInt256().Should().Be((UInt256) i);
            }
        }

        [Fact]
        public void Delete_should_delete_all_transactions()
        {
            var keys = Enumerable.Range(0, 10).Select(i => i.ToString()).ToArray();
            _memPool.Repository.DeleteItem(keys);
            _memPool.Repository.Received(1).DeleteItem(Arg.Is<string[]>(s => s.SequenceEqual(keys)));
        }

        [Fact(Skip = "don't like testing we hit a logger")]
        public void Delete_should_log_deletion_errors()
        {
            var keys = Enumerable.Range(0, 3).Select(i => i.ToString()).ToArray();
            var connectTimeoutException = new TimeoutException("that mempool connection was too slow");  
            _memPool.Repository.WhenForAnyArgs(t => t.DeleteItem(keys))
               .Throw(connectTimeoutException);

            var result = _memPool.Repository.DeleteItem(keys);

            result.Should().BeFalse();
        }

        [Fact]
        public void Get_When_Key_Not_In_Store_Should_Throw()
        {
            _memPool.Repository.ReadItem(Arg.Any<ByteString>()).ThrowsForAnyArgs(new KeyNotFoundException());
            new Action(() => _memPool.Repository.ReadItem(ByteString.CopyFromUtf8("Signature that doesn't exist")))
               .Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void SaveMempoolDocument_Should_Not_Override_Existing_Record()
        {
            // this test seems pointless like this
            
            var expectedAmount = _transactionBroadcast.PublicEntries.Single().Amount;

            _memPool.Repository.CreateItem(Arg.Is(_transactionBroadcast))
               .Returns(true);
            
            var saved = _memPool.Repository.CreateItem(_transactionBroadcast);
            saved.Should().BeTrue();
            
            var overridingTransaction = _transactionBroadcast.Clone();
            overridingTransaction.PublicEntries.Single().Amount = (expectedAmount.ToUInt256() + (UInt256) 100).ToUint256ByteString();
            
            _memPool.Repository.CreateItem(Arg.Is(overridingTransaction))
               .Returns(false);
            var overriden = _memPool.Repository.CreateItem(overridingTransaction);

            overriden.Should().BeFalse();

            _memPool.Repository.TryReadItem(Arg.Is(_transactionBroadcast.Signature.RawBytes))
               .Returns(true);
            
            var retrievedTransaction = _memPool.Repository.TryReadItem(_transactionBroadcast.Signature.RawBytes);
            retrievedTransaction.Should().BeTrue();
        }

        [Fact]
        public void SaveMempoolDocument_Should_Return_False_And_Log_On_Store_Exception()
        {
            var exception = new TimeoutException("underlying store is not connected");
            _memPool.Repository.TryReadItem(default)
               .ThrowsForAnyArgs(exception);

            var saved = _memPool.Repository.CreateItem(_transactionBroadcast);

            saved.Should().BeFalse();
        }
        
        [Fact]
        public void SaveMempoolDocument_Should_Throw_On_Document_With_Null_Transaction()
        {
            _transactionBroadcast.Signature.RawBytes = ByteString.Empty;
            
            _memPool.Repository.CreateItem(_transactionBroadcast).Throws<ArgumentNullException>();
            
            new Action(() => _memPool.Repository.CreateItem(_transactionBroadcast))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null");
        }

        [Fact]
        public void SaveMempoolDocument_Should_Throw_On_Null_Document()
        {
            _memPool.Repository.CreateItem(null).Throws<ArgumentNullException>();

            new Action(() => _memPool.Repository.CreateItem(null))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null"); // transaction is null so do not insert
        }

        [Fact]
        public void GetMempoolContent_should_return_all_documents_from_mempool()
        {
            var documentCount = 13;
            var mempoolDocs = GetTestingMempoolDocuments(documentCount);

            _memPool.Repository.GetAll().Returns(mempoolDocs);

            var content = _memPool.Repository.GetAll().ToList();

            _memPool.Repository.ReceivedWithAnyArgs(1).GetAll();

            content.Count.Should().Be(documentCount);
            content.Select(d => d.ToByteString()).Should()
               .BeEquivalentTo(mempoolDocs.Select(d => d.ToByteString()));
        }

        private static List<TransactionBroadcast> GetTestingMempoolDocuments(int documentCount)
        {
            return Enumerable.Range(0, documentCount).Select(i =>
                    TransactionHelper.GetPublicTransaction((uint) i, signature: $"key{i}"))
               .ToList();
        }

        [Fact]
        public void GetMempoolContentEncoded_should_return_an_array_of_bytes_of_strings_of_all_transactions()
        {
            var documentCount = 7;
            var mempoolDocs = GetTestingMempoolDocuments(documentCount);

            _memPool.Repository.GetAll().Returns(mempoolDocs);

            var content = _memPool.Repository.GetAll().ToList();

            _memPool.Repository.ReceivedWithAnyArgs(1).GetAll();

            content.Count.Should().Be(documentCount);
            content.Select(d => d.ToByteString()).Should()
               .BeEquivalentTo(mempoolDocs.Select(d => d.ToByteString()));
        }

        [Fact]
        public void ContainsDocument_Should_Return_True_On_Known_DocumentId()
        {
            AddKeyValueStoreEntryExpectation(_transactionBroadcast);
            _memPool.Repository.TryReadItem(_transactionBroadcast.Signature.RawBytes).Should().BeTrue();
        }

        [Fact]
        public void ContainsDocument_Should_Return_False_On_Unknown_DocumentId()
        {
            var unknownTransaction = "key not in the mempool".ToUtf8ByteString();
            _memPool.Repository.TryReadItem(unknownTransaction).Should().BeFalse();
        }
    }
}
