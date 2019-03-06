/*
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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using Catalyst.Node.Common.Modules.Mempool;
using Catalyst.Protocols.Transaction;
using Dawn;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.Modules.Mempool
{
    /// <summary>
    ///     Mempool class wraps around a IKeyValueStore
    /// </summary>
    public class Mempool : IMempool
    {
        private readonly ILogger _logger;
        private readonly IRepository<StTxModel, Key> _transactionStore;

        /// <inheritdoc />
        public Mempool(IRepository<StTxModel, Key> transactionStore, ILogger logger)
        {
            Guard.Argument(transactionStore, nameof(transactionStore)).NotNull();
            _transactionStore = transactionStore;
            _logger = logger;
            _transactionStore.CachingEnabled = true;
        }

        /// <inheritdoc />
        public IDictionary<Key, StTx> GetMemPoolContent()
        {
            var memPoolContent = _transactionStore
               .GetAll()
               .ToDictionary(tx => tx.Key, tx => tx.Transaction);
            return memPoolContent;
        }

        public bool SaveTx(Key key, StTx transaction)
        {
            Guard.Argument(key, nameof(key)).NotNull();
            Guard.Argument(transaction, nameof(transaction)).NotNull();
            return SaveTx(new StTxModel {Key = key, Transaction = transaction});
        }

        /// <inheritdoc />
        public StTx GetTx(Key key)
        {
            Guard.Argument(key, nameof(key)).NotNull();
            var found = _transactionStore.Get(key);
            return found.Transaction;
        }

        /// <inheritdoc />
        public bool SaveTx(StTxModel keyedTransaction)
        {
            Guard.Argument(keyedTransaction, nameof(keyedTransaction)).NotNull();
            try
            {
                if (_transactionStore.TryGet(keyedTransaction.Key, out _))
                {
                    return false;
                }
                _transactionStore.Add(keyedTransaction);
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