using System;
using System.Collections.Generic;
using System.Net;
using Dawn;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Newtonsoft;

namespace Catalyst.Node.Core.Components.Redis
{
    public class RedisStore : IDisposable, IRedisStore
    {
        private readonly When _when;
        private IRedisConnector _redisConnector;

        public RedisStore(When when = When.NotExists)
        {
            _when = when;
        }

        public RedisStore(string when)
        {
            Guard.Argument(when, nameof(when)).NotNull().NotEmpty().NotWhiteSpace();
            if (!Enum.TryParse(when, out _when))
            {
                throw new ArgumentException($"Invalid When setting format:{when}");
            }
        }

        public void Connect(IPEndPoint endPoint)
        {
            Guard.Argument(endPoint, nameof(endPoint)).NotNull();
            _redisConnector = new RedisConnector(endPoint.ToString());
        }

        ///<inheritdoc />
        public bool Set(byte[] key, byte[] value, TimeSpan? expiry)
        {
            Guard.Argument(key, nameof(key)).NotEmpty();
            Guard.Argument(value, nameof(value)).NotEmpty();
            return _redisConnector.Database.StringSet(key, value, expiry, _when);
        }

        ///<inheritdoc />
        public byte[] Get(byte[] value)
        {
            Guard.Argument(value, nameof(value)).NotEmpty();
            return _redisConnector.Database.StringGet(value);
        }

        public IDictionary<byte[], byte[]> GetSnapshot()
        {
            throw new NotImplementedException("On a big table that might require lots of resources.");
        }

        public IDictionary<string, string> GetInfo()
        {
            var serializer = new NewtonsoftSerializer();
            var sut = new StackExchangeRedisCacheClient(_redisConnector.Connection, serializer);

            return sut.GetInfo();
        }

        public void Dispose()
        {
            _redisConnector?.Dispose();
        }
    }
}