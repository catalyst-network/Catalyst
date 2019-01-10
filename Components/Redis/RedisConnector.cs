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
                
        /// <summary>
        /// Get the instance of this class (singleton)
        /// </summary>
        public static RedisConnector Instance()
        {
            return _instance ?? (_instance =
                       new Lazy<RedisConnector>(() => 
                           new RedisConnector("127.0.0.1 ,allowAdmin=true")).Value);
        }

        /// <summary>
        /// Get the connection multiplexer to Redis
        /// </summary>
        /// <returns>ConnectionMultiplexer</returns>
        public ConnectionMultiplexer Connection => _connection.Value;
        
        /// <summary>
        /// Get the Redis database 
        /// </summary>
        /// <returns>IDatabase</returns>
        public IDatabase GetDb => _connection.Value.GetDatabase();
    }
}