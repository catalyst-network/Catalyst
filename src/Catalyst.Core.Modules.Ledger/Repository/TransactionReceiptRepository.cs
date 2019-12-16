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
using Nethermind.Core.Crypto;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Ledger.Repository 
{
    class TransactionReceiptRepository : ITransactionReceiptRepository
    {
        readonly IRepository<TransactionReceipt, string> _repository;

        public TransactionReceiptRepository(IRepository<TransactionReceipt, string> repository) { _repository = repository; }

        public void Put(TransactionReceipt receipt)
        {
            string key = receipt.DocumentId;
            if (!_repository.TryGet(key, out _))
            {
                _repository.Add(receipt);
            }
        }

        public bool TryFind(Keccak hash, out TransactionReceipt receipt)
        {
            return _repository.TryGet(hash.ToString(), out receipt);
        }
    }
}
