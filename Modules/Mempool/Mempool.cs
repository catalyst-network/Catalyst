using System;
using ADL.Protocols.Mempool;
using Google.Protobuf;
using StackExchange.Redis;

namespace ADL.Mempool
{
    public class Mempool : IMempool
    {
        private When _when;
        
        public Mempool(IMempoolSettings settings)
        {            
//            _redisDb = RedisConnector.Instance().GetDb;
//            ParseSettings(settings);
        }
        
        private void ParseSettings(IMempoolSettings settings)
        {            
            if (!Enum.TryParse(settings.When, out _when))
            {
                throw new ArgumentException($"Invalid When setting format:{settings.When}");
            }
        }
        
        public void Save(Key k, Tx value)
        {
            // value with same key not updated -- see param When.NotExists
//            _redisDb.StringSet(k.ToByteArray(), value.ToByteArray(),null,_when);
        }

        public Tx Get(Key k)
        {
//            return Tx.Parser.ParseFrom(_redisDb.StringGet(k.ToByteArray()));
        }
    }
}
 