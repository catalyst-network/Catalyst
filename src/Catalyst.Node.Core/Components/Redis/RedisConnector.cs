using System;
using Dawn;
using StackExchange.Redis;

namespace Catalyst.Node.Core.Components.Redis
{
    public class RedisConnector : IDisposable, IRedisConnector
    {
        public RedisConnector(string connectionParam)
        {
            Guard.Argument(connectionParam, nameof(connectionParam)).NotNull().NotEmpty().NotWhiteSpace();
            Connection = ConnectionMultiplexer.ConnectAsync(connectionParam).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public IConnectionMultiplexer Connection { get; }

        /// <inheritdoc />
        public IDatabase Database => Connection.GetDatabase();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Connection?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}