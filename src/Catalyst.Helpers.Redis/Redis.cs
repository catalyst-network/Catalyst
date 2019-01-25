using System;
using System.Collections.Generic;
using System.Net;
using Catalyst.Helpers.KeyValueStore;
using Dawn;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Newtonsoft;

namespace Catalyst.Helpers.Redis
{
    public class Redis : IKeyValueStore
    {
        private RedisConnector _redisConnector;
        private When _when;

        /// <summary>
        ///     Class constructor.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="when"></param>
        public Redis(When when = When.NotExists)
        {
            _when = when;
        }

//        public IPAddress Host { get; set; }

        public void Connect(IPEndPoint host)
        {
            Guard.Argument(host, nameof(host)).NotNull();
            _redisConnector = RedisConnector.GetInstance(host);
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiry"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        public bool Set(byte[] key, byte[] value, TimeSpan? expiry)
        {
            Guard.Argument(key, nameof(key)).NotEmpty();
            Guard.Argument(value, nameof(value)).NotEmpty();
            return _redisConnector.GetDb.StringSet(key, value, expiry, _when);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public byte[] Get(byte[] value)
        {
            Guard.Argument(value, nameof(value)).NotEmpty();
            return _redisConnector.GetDb.StringGet(value);
        }

        /// <summary>
        /// </summary>
        /// <returns>Dictionary<string, string></returns>
        /// <see>https://redis.io/commands/INFO</see>
        public Dictionary<string, string> GetInfo()
        {
            var serializer = new NewtonsoftSerializer();
            var sut = new StackExchangeRedisCacheClient(_redisConnector.Connection, serializer);

            return sut.GetInfo();
        }

        /// <summary>
        /// </summary>
        /// <param name="when"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ParseSettings(string when)
        {
            Guard.Argument(when, nameof(when)).NotNull().NotEmpty().NotWhiteSpace();
            if (!Enum.TryParse(when, out _when)) throw new ArgumentException($"Invalid When setting format:{when}");
        }
    }
}