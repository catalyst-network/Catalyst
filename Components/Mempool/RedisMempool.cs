using System;
using ADL.Mempool.Proto;
using ADL.Redis;
using Google.Protobuf;
using StackExchange.Redis;

namespace ADL.Mempool
{
    public class RedisMempool : IMempool
    {
        private readonly IDatabase _redisDb;
        private When _when;

        private void ParseSettings(IMempoolSettings settings)
        {            
            if (!Enum.TryParse(settings.When, out _when))
            {
                throw new ArgumentException($"Invalid When setting format:{settings.When}");
            }
        }
        
        public RedisMempool(IMempoolSettings settings)
        {            
            _redisDb = RedisConnector.Instance().GetDb;
            ParseSettings(settings);
        }

        public void Save(Key k, Tx value)
        {
            // value with same key not updated -- see param When.NotExists
            _redisDb.StringSet(k.ToByteArray(), value.ToByteArray(),null,_when);
        }

        public Tx Get(Key k)
        {   
            return Tx.Parser.ParseFrom(_redisDb.StringGet(k.ToByteArray()));
        }
    }
}
 