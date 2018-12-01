using ADL.Mempool.Proto;

namespace ADL.Mempool
{
    public interface IMempool
    {
        void Save(Key k, Tx value);
        Tx Get(Key k);
    }
}