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

using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Ledger.Models;
using Catalyst.Abstractions.Repository;
using Catalyst.Protocol.Transaction;
using LibP2P;
using Nethermind.Core.Crypto;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Ledger.Repository 
{
    public class TransactionReceiptRepository : ITransactionReceiptRepository
    {
        readonly IRepository<TransactionReceipts, string> _repository;
        readonly IRepository<TransactionToDelta, string> _transactionToDeltaRepository;
        readonly IHashProvider _hashProvider;
        readonly IDeltaCache _deltaCache;

        public TransactionReceiptRepository(IRepository<TransactionReceipts, string> repository,
            IRepository<TransactionToDelta, string> transactionToDeltaRepository,
            IHashProvider hashProvider,
            IDeltaCache deltaCache)
        {
            _repository = repository;
            _transactionToDeltaRepository = transactionToDeltaRepository;
            _hashProvider = hashProvider;
            _deltaCache = deltaCache;
        }

        public void Put(Cid deltaHash, TransactionReceipt[] receipts, PublicEntry[] deltaPublicEntries)
        {
            if (!_repository.TryGet(GetDocumentId(deltaHash), out _))
            {
                _repository.Add(new TransactionReceipts
                {
                    DocumentId = GetDocumentId(deltaHash),
                    Receipts = receipts
                });

                for (var i = 0; i < receipts.Length; i++)
                {
                    var transactionHash = deltaPublicEntries[i].GetDocumentId(_hashProvider);
                    _transactionToDeltaRepository.Delete(transactionHash);
                    _transactionToDeltaRepository.Add(new TransactionToDelta {DeltaHash = deltaHash, DocumentId = transactionHash});
                }
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

        public bool TryFind(Keccak transactionHash, out TransactionReceipt receipt)
        {
            if (!_transactionToDeltaRepository.TryGet(transactionHash.AsDocumentId(), out var transactionToDelta))
            {
                receipt = default;
                return false;
            }

            if (!_deltaCache.TryGetOrAddConfirmedDelta(transactionToDelta.DeltaHash, out var delta))
            {
                receipt = default;
                return false;
            }

            var index = 0;
            var id = transactionHash.AsDocumentId();

            foreach (var publicEntry in delta.PublicEntries)
            {
                var documentId = publicEntry.GetDocumentId(_hashProvider);
                if (id == documentId)
                {
                    if (_repository.TryGet(GetDocumentId(transactionToDelta.DeltaHash), out var receipts))
                    {
                        receipt = receipts.Receipts[index];
                        return true;
                    }

                    receipt = default;
                    return false;
                }

                index++;
            }

            receipt = default;
            return false;
        }

        static string GetDocumentId(Cid deltaHash) => deltaHash.ToString();
    }
}
