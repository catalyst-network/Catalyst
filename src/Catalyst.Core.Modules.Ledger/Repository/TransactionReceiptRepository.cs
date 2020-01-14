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

using Catalyst.Abstractions.Ledger.Models;
using LibP2P;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Ledger.Repository 
{
    public class TransactionReceiptRepository : ITransactionReceiptRepository
    {
        readonly IRepository<TransactionReceipts, string> _repository;

        public TransactionReceiptRepository(IRepository<TransactionReceipts, string> repository) { _repository = repository; }

        public void Put(Cid deltaHash, TransactionReceipt[] receipts)
        {
            if (!_repository.TryGet(GetDocumentId(deltaHash), out _))
            {
                _repository.Add(new TransactionReceipts
                {
                    DocumentId = GetDocumentId(deltaHash),
                    Receipts = receipts
                });
            }
        }

        public bool TryFind(Cid deltaHash, out TransactionReceipt[] receipts)
        {
            if (_repository.TryGet(GetDocumentId(deltaHash), out var existing))
            {
                receipts = existing.Receipts;
                return true;
            }

            receipts = default;
            return false;
        }

        static string GetDocumentId(Cid deltaHash) => deltaHash.ToString();
    }
}
