using System;
using System.Collections.Generic;
using System.Net;
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Core.Components.Redis
{
    public interface IRedisStore : IKeyValueStore, IDisposable
    {
        /// <summary>
        ///     Connect the Redis store to a given endpoint.
        /// </summary>
        void Connect(IPEndPoint endPoint);

        /// <summary>
        ///     Get informations about the current Redis client.
        /// </summary>
        /// <see href="https://redis.io/commands/INFO" />
        IDictionary<string, string> GetInfo();
    }
}