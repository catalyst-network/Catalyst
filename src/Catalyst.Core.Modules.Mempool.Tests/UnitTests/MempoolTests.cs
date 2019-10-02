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
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Modules.Mempool.Repositories;
using Catalyst.TestUtils;
using FluentAssertions;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Core.Modules.Mempool.Tests.UnitTests
{
    public sealed class MempoolTests
    {
        private readonly Mempool _memPool;

        private readonly TransactionBroadcastDao _transactionBroadcast;

        public MempoolTests()
        {
            TestMappers.Start();

            IRepository<TransactionBroadcastDao, string> inMemoryRepository =
                new InMemoryRepository<TransactionBroadcastDao, string>();
            var mempoolDocumentRepository = new MempoolDocumentRepository(inMemoryRepository);

            _memPool = new Mempool(mempoolDocumentRepository);

            _transactionBroadcast = new TransactionBroadcastDao().ToDao(TransactionHelper.GetPublicTransaction());
        }

        [Fact]
        public void Get_should_retrieve_a_saved_transaction()
        {
            _memPool.Repository.CreateItem(_transactionBroadcast);

            var mempoolDocument =
                _memPool.Repository.Find(x => x.Signature.RawBytes == _transactionBroadcast.Signature.RawBytes);
            var expectedTransaction = _transactionBroadcast;
            var transactionFromMemPool = mempoolDocument;

            transactionFromMemPool.PublicEntries.Single().Amount.Should()
               .Be(expectedTransaction.PublicEntries.Single().Amount);
            transactionFromMemPool.Signature.RawBytes.SequenceEqual(expectedTransaction.Signature.RawBytes).Should()
               .BeTrue();
            transactionFromMemPool.TimeStamp.Should().Be(expectedTransaction.TimeStamp);
            transactionFromMemPool.SummedEntryFees().Should().Be(expectedTransaction.SummedEntryFees());
        }

        [Fact]
        public void Delete_should_delete_all_transactions()
        {
            const int numTx = 10;
            var documents = GetTestingMempoolDocuments(numTx);
            _memPool.Repository.Add(documents);

            var keys = documents.Select(i => i.Signature.RawBytes).ToList();
            _memPool.Repository.Delete(x => keys.Contains(x.Signature.RawBytes));

            _memPool.Repository.Count().Should().Be(0);
        }

        //[Fact(Skip = "don't like testing we hit a logger")]
        //public void Delete_should_log_deletion_errors()
        //{
        //    var keys = Enumerable.Range(0, 3).Select(i => i.ToString()).ToArray();
        //    var connectTimeoutException = new TimeoutException("that mempool connection was too slow");
        //    _memPool.Repository.WhenForAnyArgs(t => t.DeleteItem(keys))
        //       .Throw(connectTimeoutException);

        //    var result = _memPool.Repository.DeleteItem(keys);

        //    result.Should().BeFalse();
        //}

        //[Fact]
        //public void Get_When_Key_Not_In_Store_Should_Throw()
        //{
        //    var a = _memPool.Repository.ReadItem("Signature that doesn't exist");
        //    var b = 0;

        //    //_memPool.Repository.ReadItem(Arg.Any<string>()).ThrowsForAnyArgs(new KeyNotFoundException());
        //    //new Action(() => _memPool.Repository.ReadItem("Signature that doesn't exist"))
        //    //   .Should().Throw<KeyNotFoundException>();
        //}

        //[Fact]
        //public void SaveMempoolDocument_Should_Not_Override_Existing_Record()
        //{
        //    // this test seems pointless like this

        //    var expectedAmount = _transactionBroadcast.ToProtoBuff().PublicEntries.Single().Amount;

        //    _memPool.Repository.CreateItem(Arg.Is(_transactionBroadcast))
        //       .Returns(true);

        //    var saved = _memPool.Repository.CreateItem(_transactionBroadcast);
        //    saved.Should().BeTrue();

        //    var overridingTransaction = _transactionBroadcast.ToProtoBuff().Clone();
        //    overridingTransaction.PublicEntries.Single().Amount =
        //        (expectedAmount.ToUInt256() + (UInt256) 100).ToUint256ByteString();

        //    _memPool.Repository.CreateItem(Arg.Is(overridingTransaction))
        //       .Returns(false);
        //    var overriden = _memPool.Repository.CreateItem(overridingTransaction);

        //    overriden.Should().BeFalse();

        //    _memPool.Repository.TryReadItem(Arg.Is(_transactionBroadcast.Signature.RawBytes))
        //       .Returns(true);

        //    var retrievedTransaction = _memPool.Repository.TryReadItem(_transactionBroadcast.Signature.RawBytes);
        //    retrievedTransaction.Should().BeTrue();
        //}

        //[Fact]
        //public void SaveMempoolDocument_Should_Return_False_And_Log_On_Store_Exception()
        //{
        //    var exception = new TimeoutException("underlying store is not connected");
        //    _memPool.Repository.TryReadItem(default)
        //       .ThrowsForAnyArgs(exception);

        //    var saved = _memPool.Repository.CreateItem(_transactionBroadcast);

        //    saved.Should().BeFalse();
        //}

        [Fact]
        public void SaveMempoolDocument_Should_Throw_On_Document_With_Null_Transaction()
        {
            _transactionBroadcast.Signature.RawBytes = null;

            new Action(() => _memPool.Repository.CreateItem(_transactionBroadcast))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null");
        }

        [Fact]
        public void SaveMempoolDocument_Should_Throw_On_Null_Document()
        {
            new Action(() => _memPool.Repository.CreateItem(null))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null"); // transaction is null so do not insert
        }

        [Fact]
        public void GetMempoolContent_should_return_all_documents_from_mempool()
        {
            var documentCount = 13;
            var mempoolDocs = GetTestingMempoolDocuments(documentCount);
            _memPool.Repository.Add(mempoolDocs);

            var content = _memPool.Repository.GetAll().ToList();

            content.Count.Should().Be(documentCount);
            content.Should().BeEquivalentTo(mempoolDocs);
        }

        private static List<TransactionBroadcastDao> GetTestingMempoolDocuments(int documentCount)
        {
            return Enumerable.Range(0, documentCount).Select(i => new TransactionBroadcastDao().ToDao(
                    TransactionHelper.GetPublicTransaction((uint) i, signature: $"key{i}")))
               .ToList();
        }

        [Fact]
        public void ContainsDocument_Should_Return_True_On_Known_DocumentId()
        {
            _memPool.Repository.Add(_transactionBroadcast);
            _memPool.Repository.Exists(x => x.Signature.RawBytes == _transactionBroadcast.Signature.RawBytes).Should()
               .BeTrue();
        }

        [Fact]
        public void ContainsDocument_Should_Return_False_On_Unknown_DocumentId()
        {
            var unknownTransaction = "key not in the mempool";
            _memPool.Repository.TryReadItem(unknownTransaction).Should().BeFalse();
        }
    }
}
