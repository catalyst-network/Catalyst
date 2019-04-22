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
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Protocol.Transaction;
using Dawn;
using Nethereum.RLP;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.Modules.Mempool
{
    /// <summary>
    ///     Mempool class wraps around a IKeyValueStore
    /// </summary>
    public sealed class Mempool : IMempool
    {
        private readonly ILogger _logger;
        private readonly IRepository<Transaction, TransactionSignature> _transactionStore;

        /// <inheritdoc />
        public Mempool(IRepository<Transaction, TransactionSignature> transactionStore, ILogger logger)
        {
            Guard.Argument(transactionStore, nameof(transactionStore)).NotNull();
            _transactionStore = transactionStore;
            _transactionStore.Conventions.GetPrimaryKeyName = _ => nameof(Transaction.Signature);

            _logger = logger;
            _transactionStore.CachingEnabled = true;
        }

        /// <inheritdoc />
        public IEnumerable<Transaction> GetMemPoolContent()
        {
            var memPoolContent = _transactionStore.GetAll();
            return memPoolContent;
        }

        public List<byte[]> GetMemPoolContentEncoded()
        {
            var memPoolContent = GetMemPoolContent();

            var encodedTxs = memPoolContent.Select(tx => tx.ToString().ToBytesForRLPEncoding()).ToList();

            return encodedTxs;
        }

        /// <inheritdoc />
        public Transaction GetTransaction(TransactionSignature key)
        {
            Guard.Argument(key, nameof(key)).NotNull();
            var found = _transactionStore.Get(key);
            return found;
        }

        /// <inheritdoc />
        public bool SaveTransaction(Transaction transaction)
        {
            Guard.Argument(transaction, nameof(transaction)).NotNull();
            Guard.Argument(transaction.Signature, nameof(transaction.Signature)).NotNull();
            try
            {
                if (_transactionStore.TryGet(transaction.Signature, out _))
                {
                    return false;
                }

                _transactionStore.Add(transaction);
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to add standard transaction to mempool");
                return false;
            }
        }
    }
}
