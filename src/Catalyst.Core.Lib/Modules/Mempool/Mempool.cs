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
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.Modules.Mempool.Models;
using Catalyst.Protocol.Transaction;
using Dawn;
using Google.Protobuf;
using Nethereum.RLP;
using Serilog;

namespace Catalyst.Core.Lib.Modules.Mempool
{
    /// <summary>
    ///     Mempool class wraps around a IKeyValueStore
    /// </summary>
    public sealed class Mempool : IMempool
    {
        private readonly ILogger _logger;
        private readonly IMempoolRepository _transactionStore;

        /// <inheritdoc />
        public Mempool(IMempoolRepository transactionStore, ILogger logger)
        {
            Guard.Argument(transactionStore, nameof(transactionStore)).NotNull();
            _transactionStore = transactionStore;
            _logger = logger;
        }

        /// <inheritdoc />
        public IEnumerable<IMempoolDocument> GetMemPoolContent()
        {
            var memPoolContent = _transactionStore.GetAll();
            return memPoolContent;
        }

        public bool ContainsDocument(TransactionSignature key)
        {
            return _transactionStore.TryGet(key.ToByteString().ToBase64(), out _);
        }

        public List<byte[]> GetMemPoolContentEncoded()
        {
            var memPoolContent = GetMemPoolContent();

            var encodedTxs = memPoolContent.Select(tx => tx.ToString().ToBytesForRLPEncoding()).ToList();

            return encodedTxs;
        }

        /// <inheritdoc />
        public IMempoolDocument GetMempoolDocument(TransactionSignature key)
        {
            Guard.Argument(key, nameof(key)).NotNull();
            var found = _transactionStore.Get(key.ToByteString().ToBase64());
            return found;
        }

        public void Clear()
        {
            _logger.Debug("Clearing mempool");
            var keysToDelete = _transactionStore.GetAll(e => e.DocumentId).ToArray();
            var cleared = 0;

            foreach (var key in keysToDelete)
            {
                try
                {
                    _transactionStore.Delete(key);
                    cleared++;
                }
                catch (Exception e)
                {
                    _logger.Warning(e, "Could not delete transaction {0} from mempool.", key);
                }    
            }

            _logger.Debug("Cleared {cleared} out of {total} transactions from the mempool.", 
                cleared, keysToDelete.Length);
        }

        /// <inheritdoc />
        public bool SaveMempoolDocument(IMempoolDocument mempoolDocument)
        {
            Guard.Argument(mempoolDocument, nameof(mempoolDocument)).NotNull();

            var transaction = mempoolDocument.Transaction;
            Guard.Argument(transaction, nameof(transaction)).NotNull();
            Guard.Argument(transaction.Signature, nameof(transaction.Signature)).NotNull();
            try
            {
                if (_transactionStore.TryGet(mempoolDocument.DocumentId, out _))
                {
                    return false;
                }

                _transactionStore.Add((MempoolDocument) mempoolDocument);
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
