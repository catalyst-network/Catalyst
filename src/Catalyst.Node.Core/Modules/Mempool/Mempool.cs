using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Catalyst.Node.Common.Modules.Mempool;
using Catalyst.Protocols.Transaction;
using Dawn;
using Google.Protobuf;
using Serilog;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.Modules.Mempool
{
    /// <summary>
    ///     Mempool class wraps around a IKeyValueStore
    /// </summary>
    public class Mempool : IMempool
    {
        private readonly ICrudRepository<StTx, Key> _keyValueStore;
        private readonly ILogger _logger;

        /// <inheritdoc />
        public Mempool(ICrudRepository<StTx, Key> keyValueStore, ILogger logger)
        {
            Guard.Argument(keyValueStore, nameof(keyValueStore)).NotNull()
                .Require(store => store.GetType().IsAssignableFrom(typeof(InMemoryRepository<StTxModel>)));
            _keyValueStore = keyValueStore;
            _logger = logger;
            _keyValueStore.CachingEnabled = true;
        }

        /// <inheritdoc />
        public IDictionary<Key, StTx> GetMemPoolContent()
        {
            var memPoolContent = _keyValueStore
               .GetAll()
               .ToDictionary(tx => tx.Key, tx => tx.Transaction);
            return memPoolContent;
        }

        public bool SaveTx(Key key, StTx transaction)
        {
            Guard.Argument(key, nameof(key)).NotNull();
            Guard.Argument(transaction, nameof(transaction)).NotNull();
            return SaveTx(new StTxModel() {Key = key, Transaction = transaction});
        }

        /// <inheritdoc />
        public bool SaveTx(StTxModel keyedTransaction)
        {
            Guard.Argument(keyedTransaction, nameof(keyedTransaction)).NotNull();
            try
            {
                _keyValueStore.Add(keyedTransaction);
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to add standard transaction to mempool");
                return false;
            }
        }

        /// <inheritdoc />
        public StTx GetTx(Key key)
        {
            Guard.Argument(key, nameof(key)).NotNull();
            var found = _keyValueStore.Get(r => r.Key.ToByteArray().SequenceEqual(key.ToByteArray()), out StTxModel stTxModel);
            if (!found) throw new KeyNotFoundException(key.ToString());
            return stTxModel.Transaction;
        }
    }
}