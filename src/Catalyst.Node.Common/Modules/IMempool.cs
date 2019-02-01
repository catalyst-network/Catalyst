using System.Collections.Generic;
using Catalyst.Protocols.Mempool;

namespace Catalyst.Node.Common.Modules
{
    public interface IMempool
    {
        IKeyValueStore _keyValueStore { get; set; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> GetMempool();

        /// <summary>
        /// </summary>
        /// <param name="k"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool SaveTx(Key k, Tx value);

        /// <summary>
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        Tx GetTx(Key k);
    }
}