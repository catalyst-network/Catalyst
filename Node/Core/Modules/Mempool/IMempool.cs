using ADL.Protocol.Mempool.dist.csharp;

namespace ADL.Node.Core.Modules.Mempool
{
    public interface IMempool
    {
        bool SaveTx(Key k, Tx value);
        Tx GetTx(Key k);
    }
}