using System;
using StackExchange.Redis;
using Google.Protobuf;

namespace ADL.Redis
{
    public class Redis
    {
        private readonly IDatabase _redisDb = RedisConnector.Instance.GetDb;

        public bool Set(IMessage key, IMessage value, TimeSpan? expiary, When  when)
        {
            _redisDb.StringSet(key.ToByteArray(), value.ToByteArray(), null, when);
            return true;
        }
    }
}