using System;
using StackExchange.Redis;

namespace Catalyst.Node.Core.Components.Redis
{
    /// <summary>
    ///     Wrapper around Redis Connection multiplexer
    /// </summary>
    public interface IRedisConnector : IDisposable
    {
        /// <summary>
        ///     Gets the connection multiplexer to Catalyst.Helpers.Redis
        /// </summary>
        /// <returns>ConnectionMultiplexer</returns>
        IConnectionMultiplexer Connection { get; }

        /// <summary>
        ///     Gets the Catalyst.Helpers.Redis database
        /// </summary>
        /// <returns>IDatabase</returns>
        IDatabase Database { get; }
    }
}