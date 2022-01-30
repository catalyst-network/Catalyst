#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.Mempool.Services;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using MultiFormats.Registry;
using Nethermind.Dirichlet.Numerics;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Mempool.Tests.UnitTests
{
    public sealed class MempoolTests
    {
        private Mempool _memPool;
        private PublicEntryDao _mempoolItem;
        private TestMapperProvider _mapperProvider;

        [SetUp]
        public void Init()
        {
            new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            _memPool = new Mempool(Substitute.For<IMempoolService<PublicEntryDao>>());
            _mapperProvider = new TestMapperProvider();
            _mempoolItem = TransactionHelper
               .GetPublicTransaction().PublicEntry
               .ToDao<PublicEntry, PublicEntryDao>(_mapperProvider);
        }

        private void AddKeyValueStoreEntryExpectation(PublicEntryDao mempoolItem)
        {
            _memPool.Service.ReadItem(Arg.Is<string>(k => k == mempoolItem.Id))
               .Returns(mempoolItem);

            _memPool.Service.TryReadItem(Arg.Is<string>(k => k == mempoolItem.Id))
               .Returns(true);
        }

        [Test]
        public void Get_should_retrieve_a_saved_transaction()
        {
            _memPool.Service.CreateItem(_mempoolItem);
            AddKeyValueStoreEntryExpectation(_mempoolItem);
            
            var mempoolDocument = _memPool.Service.ReadItem(_mempoolItem.Id).ToProtoBuff<PublicEntryDao, PublicEntry>(_mapperProvider);
            var expectedTransaction = _mempoolItem.ToProtoBuff<PublicEntryDao, PublicEntry>(_mapperProvider);

            mempoolDocument.Amount.ToUInt256().Should().Be(expectedTransaction.Amount.ToUInt256());
            mempoolDocument.Signature.RawBytes.SequenceEqual(expectedTransaction.Signature.RawBytes).Should().BeTrue();
            mempoolDocument.GasPrice.ToUInt256().Should().Be(expectedTransaction.GasPrice.ToUInt256());
        }

        [Test]
        public void Get_should_retrieve_saved_transaction_matching_their_keys()
        {
            const int numTx = 10;
            var documents = GetTestingMempoolDocuments(numTx);
            documents.ForEach(AddKeyValueStoreEntryExpectation);

            for (var i = 0; i < numTx; i++)
            {
                var mempoolDocument = _memPool.Service.ReadItem(documents[i].Id).ToProtoBuff<PublicEntryDao, PublicEntry>(_mapperProvider);
                mempoolDocument.Amount.ToUInt256().Should().Be((UInt256) i);
            }
        }

        [Test]
        public void Delete_should_delete_all_transactions()
        {
            var keys = Enumerable.Range(0, 10).Select(i => i.ToString()).ToArray();
            _memPool.Service.DeleteItem(keys);
            _memPool.Service.Received(1).DeleteItem(Arg.Is<string[]>(s => s.SequenceEqual(keys)));
        }

        [Ignore("don't like testing we hit a logger")]
        public void Delete_should_log_deletion_errors()
        {
            var keys = Enumerable.Range(0, 3).Select(i => i.ToString()).ToArray();
            TimeoutException connectTimeoutException = new("that mempool connection was too slow");
            _memPool.Service.WhenForAnyArgs(t => t.DeleteItem(keys))
               .Throw(connectTimeoutException);

            var result = _memPool.Service.DeleteItem(keys);

            result.Should().BeFalse();
        }

        [Test]
        public void Get_When_Key_Not_In_Store_Should_Throw()
        {
            _memPool.Service.ReadItem(Arg.Any<string>()).ThrowsForAnyArgs(new KeyNotFoundException());
            new Action(() => _memPool.Service.ReadItem("Signature that doesn't exist"))
               .Should().Throw<KeyNotFoundException>();
        }

        [Test]
        public void SaveMempoolDocument_Should_Not_Override_Existing_Record()
        {
            // this test seems pointless like this

            var expectedAmount = _mempoolItem.ToProtoBuff<PublicEntryDao, PublicEntry>(_mapperProvider).Amount;

            _memPool.Service.CreateItem(Arg.Is(_mempoolItem))
               .Returns(true);

            var saved = _memPool.Service.CreateItem(_mempoolItem);
            saved.Should().BeTrue();

            var overridingTransaction = _mempoolItem.ToProtoBuff<PublicEntryDao, PublicEntry>(_mapperProvider).Clone();

            overridingTransaction.Amount = (expectedAmount.ToUInt256() + (UInt256) 100).ToUint256ByteString();

            var overridingTransactionDao = overridingTransaction.ToDao<PublicEntry, PublicEntryDao>(_mapperProvider);
            _memPool.Service.CreateItem(Arg.Is(overridingTransactionDao)).Returns(false);
            var overriden = _memPool.Service.CreateItem(overridingTransactionDao);

            overriden.Should().BeFalse();

            _memPool.Service.TryReadItem(Arg.Is(_mempoolItem.Signature.RawBytes))
               .Returns(true);

            var retrievedTransaction = _memPool.Service.TryReadItem(_mempoolItem.Signature.RawBytes);
            retrievedTransaction.Should().BeTrue();
        }

        [Test]
        public void SaveMempoolDocument_Should_Return_False_And_Log_On_Store_Exception()
        {
            TimeoutException exception = new("underlying store is not connected");
            _memPool.Service.TryReadItem(default)
               .ThrowsForAnyArgs(exception);

            var saved = _memPool.Service.CreateItem(_mempoolItem);

            saved.Should().BeFalse();
        }

        [Test]
        public void SaveMempoolDocument_Should_Throw_On_Document_With_Null_Transaction()
        {
            _mempoolItem.Signature.RawBytes = null;

            _memPool.Service.CreateItem(_mempoolItem).Throws<ArgumentNullException>();

            new Action(() => _memPool.Service.CreateItem(_mempoolItem))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null");
        }

        [Test]
        public void SaveMempoolDocument_Should_Throw_On_Null_Document()
        {
            _memPool.Service.CreateItem(null).Throws<ArgumentNullException>();

            new Action(() => _memPool.Service.CreateItem(null))
               .Should().Throw<ArgumentNullException>()
               .And.Message.Should().Contain("cannot be null"); // transaction is null so do not insert
        }

        [Test]
        public void GetMempoolContent_should_return_all_documents_from_mempool()
        {
            var documentCount = 13;
            var mempoolDocs = GetTestingMempoolDocuments(documentCount);

            _memPool.Service.GetAll().Returns(mempoolDocs);

            var content = _memPool.Service.GetAll().ToList();

            _memPool.Service.ReceivedWithAnyArgs(1).GetAll();

            content.Count.Should().Be(documentCount);
            content.Should().BeEquivalentTo(mempoolDocs);
        }

        private List<PublicEntryDao> GetTestingMempoolDocuments(int documentCount)
        {
            return Enumerable.Range(0, documentCount).Select(i =>
                TransactionHelper.GetPublicTransaction((uint) i, signature: $"key{i}").ToDao<TransactionBroadcast, TransactionBroadcastDao>(_mapperProvider)).Select(x => x.PublicEntry).ToList();
        }

        [Test]
        public void GetMempoolContentEncoded_should_return_an_array_of_bytes_of_strings_of_all_transactions()
        {
            var documentCount = 7;
            var mempoolDocs = GetTestingMempoolDocuments(documentCount);

            _memPool.Service.GetAll().Returns(mempoolDocs);

            var content = _memPool.Service.GetAll().ToList();

            _memPool.Service.ReceivedWithAnyArgs(1).GetAll();

            content.Count.Should().Be(documentCount);
            content.Should().BeEquivalentTo(mempoolDocs);
        }

        [Test]
        public void ContainsDocument_Should_Return_True_On_Known_DocumentId()
        {
            AddKeyValueStoreEntryExpectation(_mempoolItem);
            _memPool.Service.TryReadItem(_mempoolItem.Id).Should().BeTrue();
        }

        [Test]
        public void ContainsDocument_Should_Return_False_On_Unknown_DocumentId()
        {
            var unknownTransaction = "key not in the mempool";
            _memPool.Service.TryReadItem(unknownTransaction).Should().BeFalse();
        }
    }
}
