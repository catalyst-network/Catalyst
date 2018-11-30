using System;
using StackExchange.Redis;

namespace ADL.Redis
{
    public class RedisConnector
    {
        private static RedisConnector _instance;
        private static Lazy<ConnectionMultiplexer> _connection;
        
        private RedisConnector(string connectionParam)
        {
            _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connectionParam));
        }

        public static RedisConnector Instance()
        {
            return _instance ?? (_instance =
                       new Lazy<RedisConnector>(() => 
                           new RedisConnector("localhost,allowAdmin=true")).Value);
        }

        public ConnectionMultiplexer Connection => _connection.Value;
        public IDatabase GetDb => _connection.Value.GetDatabase();
    }
}