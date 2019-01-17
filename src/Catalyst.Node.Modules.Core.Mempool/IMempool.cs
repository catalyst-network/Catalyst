using System.Collections.Generic;
using Catalyst.Protocols.Mempool;

namespace Catalyst.Node.Modules.Core.Mempool
{
    public interface IMempool
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> GetMempool();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool SaveTx(Key k, Tx value);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        Tx GetTx(Key k);
    }
}
