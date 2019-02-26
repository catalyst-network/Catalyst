using System.Collections.Generic;
using Catalyst.Protocols.Transaction;

namespace Catalyst.Node.Common.Modules.Mempool
{
    public interface IMempool
    {
        /// <summary>
        ///     The key/value store used by the Mempool.
        /// </summary>
        IKeyValueStore KeyValueStore { get; }

        /// <summary>
        ///     Gets a snapshot of the current mempool content.
        /// </summary>
        /// <returns></returns>
        IDictionary<Key, StTx> GetMemPoolContent();

        /// <summary>
        ///     Saves the transaction associated with a given key.
        /// </summary>
        /// <param name="key">Key under which the transaction is stored.</param>
        /// <param name="transaction"></param>
        bool SaveTx(Key key, StTx transaction);

        /// <summary>
        ///     Retrieves the transaction corresponding the a given key.
        /// </summary>
        /// <param name="key">Key under which the transaction is stored.</param>
        /// <returns>The transaction matching the <see cref="key" /> if any.</returns>
        StTx GetTx(Key key);
    }
}