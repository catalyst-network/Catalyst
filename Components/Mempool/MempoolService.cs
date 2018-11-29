using ADL.Mempool.Proto;
using ADL.Utilities;
using Google.Protobuf;
using StackExchange.Redis;

namespace ADL.Mempool
{
    public class MempoolService : IMempool
    {
        private readonly IDatabase _redisDb = RedisConnector.Instance.GetDb;
        
        public virtual void Save(Key k, Tx value)
        {
            // value with same key not updated -- see param When.NotExists
            _redisDb.StringSet(k.ToByteArray(), value.ToByteArray(),null,When.NotExists);
        }

        public virtual Tx Get(Key k)
        {   
            return Tx.Parser.ParseFrom(_redisDb.StringGet(k.ToByteArray()));
        }
    }
}
