using ADL.Mempool.Proto;
using ADL.Utilities;
using Google.Protobuf;

namespace ADL.Mempool
{
    public class MempoolService : IMempool
    {
        public virtual void Save(Key k, Tx value)
        {
            RedisConnector.Instance.GetDb.StringSet(k.ToByteArray(), value.ToByteArray());
        }

        public virtual Tx Get(Key k)
        {   
            return Tx.Parser.ParseFrom(RedisConnector.Instance.GetDb.StringGet(k.ToByteArray()));
        }
    }
}
