using Dawn;
using StackExchange.Redis;

namespace Catalyst.Node.Core.Components.Redis
{
    public class RedisConnector : IRedisConnector
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

        /// <inheritdoc />
        public void Dispose()
        {
            Connection?.Dispose();
        }
    }
}