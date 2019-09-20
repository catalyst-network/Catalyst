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
using Catalyst.Abstractions.Repository;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Mempool.Documents;
using Catalyst.Core.Lib.Repository;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Mempool.Repositories
{
    public class MempoolDocumentRepository : RepositoryWrapper<TransactionBroadcastDao>, IMempoolRepository<TransactionBroadcastDao>
    {
        public MempoolDocumentRepository(IRepository<TransactionBroadcastDao, string> repository) : base(repository) { }

        /// <inheritdoc />
        public bool TryReadItem(ByteString signature)
        {
            return Repository.TryGet(signature.ToBase64(), out _);
        }
        
        public TransactionBroadcastDao ReadItem(ByteString signature)
        {
            return Repository.Get(signature.ToBase64());
        }

        /// <inheritdoc />
        public bool DeleteItem(params string[] transactionSignatures)
        {
            try
            {
                Repository.Delete(transactionSignatures);
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to delete transactions from the mempool {transactionSignatures}",
                    transactionSignatures);
                return false;
            }
            
            return true;
        }
        
        public bool CreateItem(TransactionBroadcastDao transactionBroadcastDao)
        {      
            if (transactionBroadcastDao.Signature.Equals(null) || transactionBroadcastDao.Signature.Equals(ByteString.Empty))
            {
                throw new ArgumentNullException(nameof(transactionBroadcastDao));
            }
            
            try
            {
                Repository.Add(transactionBroadcastDao);
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, e.Message);
                return false;
            }

            return true;
        }

        public new IEnumerable<TransactionBroadcastDao> GetAll()
        {
            return Repository.GetAll().ToList();
        }
    }
}
