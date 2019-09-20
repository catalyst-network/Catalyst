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
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Repository;
using Google.Protobuf;
using SharpRepository.Repository;

namespace Catalyst.TestUtils
{
    public sealed class TestMempoolDocumentRepository : RepositoryWrapper<TransactionBroadcastDao>,
        IMempoolRepository<TransactionBroadcastDao>
    {
        public TestMempoolDocumentRepository(IRepository<TransactionBroadcastDao, string> repository) : base(repository)
        {
        }

        public bool TryReadItem(ByteString key)
        {
            throw new NotImplementedException();
        }

        public TransactionBroadcastDao ReadItem(ByteString key)
        {
            throw new NotImplementedException();
        }

        public bool DeleteItem(params string[] transactionSignatures)
        {
            throw new NotImplementedException();
        }

        public bool CreateItem(TransactionBroadcastDao transactionBroadcast)
        {
            throw new NotImplementedException();
        }

        public new IEnumerable<TransactionBroadcastDao> GetAll()
        {
            var utcNow = DateTime.UtcNow;
            var tenSecondSlot = 1 + utcNow.Second / 10;
            var tx = TransactionHelper.GetPublicTransaction(timestamp: (long) utcNow.ToOADate());
            return Enumerable.Repeat(tx, tenSecondSlot).Select(x => new TransactionBroadcastDao().ToDao(x));
        }
    }
}
