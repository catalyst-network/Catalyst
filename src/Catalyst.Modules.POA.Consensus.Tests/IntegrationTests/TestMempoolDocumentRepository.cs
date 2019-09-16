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
using Catalyst.Core.Lib.Mempool.Documents;
using Catalyst.Core.Lib.Repository;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using Google.Protobuf;
using SharpRepository.Repository;

namespace Catalyst.Modules.POA.Consensus.Tests.IntegrationTests
{
    internal sealed class TestMempoolDocumentRepository : RepositoryWrapper<MempoolDocument>, IMempoolRepository<MempoolDocument>
    {
        internal TestMempoolDocumentRepository(IRepository<MempoolDocument, string> repository) : base(repository) { }

        public bool TryReadItem(ByteString key) { throw new NotImplementedException(); }
        public MempoolDocument ReadItem(ByteString key) { throw new NotImplementedException(); }
        public bool DeleteItem(params string[] transactionSignatures) { throw new NotImplementedException(); }
        public bool CreateItem(TransactionBroadcast transactionBroadcast) { throw new NotImplementedException(); }

        public new IEnumerable<TransactionBroadcast> GetAll()
        {
            var utcNow = DateTime.UtcNow;
            var tenSecondSlot = 1 + utcNow.Second / 10;
            var tx = TransactionHelper.GetTransaction(timeStamp: (long) utcNow.ToOADate());
            return Enumerable.Repeat(tx, tenSecondSlot);
        }
    }
}
