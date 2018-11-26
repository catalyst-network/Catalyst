using System;
using StackExchange.Redis;

namespace ADL.Utilities
{
    public class RedisConnector
    {
        private static RedisConnector _instance;
        private static Lazy<ConnectionMultiplexer> _connection;

        private RedisConnector()
        {
            _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect("localhost,allowAdmin=true"));
        }

        public static RedisConnector Instance => _instance ?? (_instance = new RedisConnector());
        public ConnectionMultiplexer Connection => _connection.Value;
        public IDatabase GetDb => _connection.Value.GetDatabase();
    }
}