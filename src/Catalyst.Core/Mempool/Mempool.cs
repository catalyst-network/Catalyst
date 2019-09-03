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
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.Mempool.Documents;
using Catalyst.Abstractions.Mempool.Repositories;
using Catalyst.Core.Mempool.Documents;
using Catalyst.Protocol.Transaction;
using Dawn;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.Mempool
{
    /// <summary>
    ///     Mempool class wraps around a IKeyValueStore
    /// </summary>
    public sealed class Mempool : IMempool<MempoolDocument>
    {
        private readonly ILogger _logger;
        public IMempoolRepository<MempoolDocument> Repository { get; }

        /// <inheritdoc />
        public Mempool(IMempoolRepository<MempoolDocument> transactionStore, ILogger logger)
        {
            Guard.Argument(transactionStore, nameof(transactionStore)).NotNull();
            Repository = transactionStore;
            _logger = logger;
        }

        /// <inheritdoc />
        private IEnumerable<TransactionBroadcast> GetMemPoolContent()
        {
            var memPoolContent = Repository.GetAll();
            return memPoolContent;
        }

        /// <inheritdoc />
        public bool ContainsDocument(TransactionSignature key)
        {
            return Repository.TryGet(key.ToByteString().ToBase64(), out _);
        }

        /// <inheritdoc />
        public List<TransactionBroadcast> GetMemPoolContentAsTransactions()
        {
            var memPoolContent = GetMemPoolContent();

            var encodedTxs = memPoolContent
               .Select(tx => tx)
               .ToList();

            return encodedTxs;
        }

        /// <inheritdoc />
        public IMempoolDocument GetMempoolDocument(TransactionSignature key)
        {
            Guard.Argument(key, nameof(key)).NotNull();
            var found = Repository.Get(key.ToByteString().ToBase64());
            return found;
        }

        /// <inheritdoc />
        public void Delete(params string[] transactionSignatures)
        {
            try
            {
                Repository.Delete(transactionSignatures);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to delete transactions from the mempool {transactionSignatures}",
                    transactionSignatures);
            }
        }

        /// <inheritdoc />
        public bool SaveMempoolDocument(IMempoolDocument mempoolDocument)
        {
            Guard.Argument(mempoolDocument, nameof(mempoolDocument)).NotNull();
            Guard.Argument(mempoolDocument.Transaction, nameof(mempoolDocument.Transaction)).NotNull();

            try
            {
                if (Repository.TryGet(mempoolDocument.DocumentId, out _))
                {
                    return false;
                }

                Repository.Add((MempoolDocument) mempoolDocument);
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
