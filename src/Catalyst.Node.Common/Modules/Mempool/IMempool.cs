using System.Collections.Generic;
using Catalyst.Protocols.Transaction;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;

namespace Catalyst.Node.Common.Modules.Mempool
{
    public interface IMempool
    {
        
        /// <summary>
        /// Gets a snapshot of the current mempool content.
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
        /// Retrieves the transaction corresponding the a given key.
        /// </summary>
        /// <param name="key">Key under which the transaction is stored.</param>
        /// <returns>The transaction matching the <see cref="key" /> if any.</returns>
        StTx GetTx(Key key);
    }
}