using System.Collections.Generic;
using System.Linq;
using ADL.Protocols.Mempool;

namespace ADL.Node.Core.Modules.Mempool
{
    public interface IMempool
    {
        Dictionary<string, string> GetInfo();
        bool SaveTx(Key k, Tx value);
        Tx GetTx(Key k);
    }
}