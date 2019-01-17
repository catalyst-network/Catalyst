using System;
using System.Net;
using StackExchange.Redis;

namespace Catalyst.Helpers.Redis
{
    public class RedisConnector
    {
        private static RedisConnector _instance;
        private static Lazy<ConnectionMultiplexer> _connection;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionParam"></param>
        private RedisConnector(string connectionParam)
        {
            //@TODO guard util
            _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connectionParam));
        }
                
        /// <summary>
        /// Get the instance of this class (singleton)
        /// </summary>
        public static RedisConnector GetInstance(IPAddress host)
        {
            //@TODO guard util
            return _instance ?? (_instance =
                       new Lazy<RedisConnector>(() => 
                           new RedisConnector($"{host},allowAdmin=true")).Value);
        }

        /// <summary>
        /// Get the connection multiplexer to Catalyst.Helpers.Redis
        /// </summary>
        /// <returns>ConnectionMultiplexer</returns>
        public ConnectionMultiplexer Connection => _connection.Value;
        
        /// <summary>
        /// Get the Catalyst.Helpers.Redis database 
        /// </summary>
        /// <returns>IDatabase</returns>
        public IDatabase GetDb => _connection.Value.GetDatabase();
    }
}