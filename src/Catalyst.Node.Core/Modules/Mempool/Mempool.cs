using System.Collections.Generic;
using System.Linq;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Modules;
using Catalyst.Protocols.Transaction;
using Dawn;
using Google.Protobuf;

namespace Catalyst.Node.Core.Modules.Mempool
{
    /// <summary>
    ///     Mempool class wraps around a IKeyValueStore
    /// </summary>
    public class Mempool : IMempool
    {
        /// <inheritdoc />
        public Mempool(IKeyValueStore keyValueStore)
        {
            Guard.Argument(keyValueStore, nameof(keyValueStore)).NotNull();
            KeyValueStore = keyValueStore;
        }

        public IKeyValueStore KeyValueStore { get; }

        /// <inheritdoc />
        public IDictionary<Key, StTx> GetMemPoolContent()
        {
            return KeyValueStore.GetSnapshot().ToDictionary(
                p => Key.Parser.ParseFrom(p.Key),
                p => StTx.Parser.ParseFrom(p.Value));
        }

        /// <inheritdoc />
        public bool SaveTx(Key k, StTx value)
        {
            Guard.Argument(k, nameof(k)).NotNull();
            Guard.Argument(value, nameof(value)).NotNull();
            return KeyValueStore.Set(k.ToByteArray(), value.ToByteArray(), null);
        }

        /// <inheritdoc />
        public StTx GetTx(Key k)
        {
            Guard.Argument(k, nameof(k)).NotNull();
            return StTx.Parser.ParseFrom(KeyValueStore.Get(k.ToByteArray()));
        }
    }
}