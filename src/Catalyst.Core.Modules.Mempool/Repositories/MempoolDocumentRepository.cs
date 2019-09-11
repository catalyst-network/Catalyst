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
using Google.Protobuf;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Mempool.Repositories
{
    public class MempoolDocumentRepository : RepositoryWrapper<MempoolDocument>, IMempoolRepository<MempoolDocument>
    {
        public MempoolDocumentRepository(IRepository<MempoolDocument, string> repository) : base(repository) { }

        /// <inheritdoc />
        public bool TryReadItem(ByteString signature)
        {
            return Repository.TryGet(signature.ToBase64(), out _);
        }
        
        public MempoolDocument ReadItem(ByteString signature)
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
        
        public bool CreateItem(TransactionBroadcast transactionBroadcast)
        {      
            if (transactionBroadcast.Signature.Equals(null) || transactionBroadcast.Signature.Equals(ByteString.Empty))
            {
                throw new ArgumentNullException(nameof(transactionBroadcast));
            }
            
            try
            {
                Repository.Add(new MempoolDocument {Transaction = transactionBroadcast});
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, e.Message);
                return false;
            }

            return true;
        }

        public new IEnumerable<TransactionBroadcast> GetAll()
        {
            return Repository.GetAll().Select(md => md.Transaction).ToList();
        }
    }
}
