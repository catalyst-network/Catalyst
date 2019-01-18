using System;
using System.Net;
using StackExchange.Redis;

namespace Catalyst.Helpers.Redis
{
    public class RedisConnector
    {
        private static RedisConnector _instance;
        private static Lazy<ConnectionMultiplexer> _connection;
        private readonly string _connectionParam;

        /// <summary>
        /// </summary>
        /// <param name="connectionParam"></param>
        private RedisConnector(string connectionParam)
        {
            if (string.IsNullOrEmpty(connectionParam))
                throw new ArgumentException("Value cannot be null or empty.", nameof(connectionParam));
            if (string.IsNullOrWhiteSpace(connectionParam))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionParam));
//            Guard.NotNull(connectionParam, nameof(connectionParam));
            _connectionParam = connectionParam;
            //@TODO guard util
            _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(_connectionParam));
        }

        /// <summary>
        ///     Get the connection multiplexer to Catalyst.Helpers.Redis
        /// </summary>
        /// <returns>ConnectionMultiplexer</returns>
        public ConnectionMultiplexer Connection => _connection.Value;

        /// <summary>
        ///     Get the Catalyst.Helpers.Redis database
        /// </summary>
        /// <returns>IDatabase</returns>
        public IDatabase GetDb => _connection.Value.GetDatabase();

        /// <summary>
        ///     Get the instance of this class (singleton)
        /// </summary>
        public static RedisConnector GetInstance(IPEndPoint host)
        {
            //@TODO guard util
            return _instance ?? (_instance =
                       new Lazy<RedisConnector>(() =>
                           new RedisConnector($"{host.Address},allowAdmin=true")).Value);
        }
    }
}