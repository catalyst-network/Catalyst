using System;
using ADL.DataStore;
using Google.Protobuf;
using StackExchange.Redis;

namespace ADL.Redis
{
    public class Redis : IKeyStore
    {
        private When _when;
        private readonly IDatabase _redisDb = RedisConnector.Instance().GetDb;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="when"></param>
        public Redis(When when = When.Always)
        {
            _when = when;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiry"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        public bool Set(byte[] key, byte[] value, TimeSpan? expiry)
        {
            return _redisDb.StringSet(key, value, expiry, _when);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public byte[] Get(byte[] value)
        {
            return _redisDb.StringGet(value);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="when"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ParseSettings(string  when)
        {            
            if (!Enum.TryParse(when, out _when))
            {
                throw new ArgumentException($"Invalid When setting format:{when}");
            }
        }
    }
}
