using System;
using System.Net;
using System.Text;
using System.Threading;
using Catalyst.Node.Core.Components.Redis;
using Catalyst.Node.Core.Helpers.Network;
using FluentAssertions;
using StackExchange.Redis;
using Xunit;

namespace Catalyst.Node.UnitTests.Helpers.Redis
{
    public class UT_RedisConnector : IDisposable
    {
        public UT_RedisConnector()
        {
            _connector = new RedisConnector(EndPoint.ToString());
            _database = _connector.Database;
        }

        public void Dispose()
        {
            _connector.Dispose();
        }

        private static readonly IPEndPoint EndPoint = EndpointBuilder.BuildNewEndPoint("127.0.0.1", 6379);

        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RedisConnector _connector;
        private readonly IDatabase _database;

        private void Writer(int start, int end)
        {
            for (var k = start; k < end; k++) _database.StringSet($"mykey:{k}", k).Should().BeTrue();
        }

        private void Reader(int start, int end)
        {
            for (var k = start; k < end; k++) _database.StringGet($"mykey:{k}").Should().Be(k);
        }

        [Fact]
        public void Connection_Should_Have_One_Endpoint()
        {
            var endpoint = _connector.Connection.GetEndPoints();
            endpoint.Length.Should().Be(1);
        }

        [Fact]
        public void KeyAlreadyExistUpdate()
        {
            _database.StringSet("mykey", 100).Should().BeTrue();
            _database.StringGet("mykey").Should().Be(100);
            _database.StringSet("mykey", 200).Should().BeTrue();
            _database.StringGet("mykey").Should().Be(200);
        }

        [Fact]
        public void KeyDoesNotExists()
        {
            var randomUnknownKey = Guid.NewGuid().ToString();
            var ret = _database.StringGet(randomUnknownKey);
            ret.HasValue.Should().BeFalse("this random key should never have been inserted in Redis before.");
        }

        [Fact]
        public void MultipleClient()
        {
            const int threadNum = 5;
            var threadW = new Thread[threadNum];
            var threadR = new Thread[threadNum];

            // Set up writer and reader
            for (var i = 0; i < threadNum; i++)
            {
                var i1 = i;
                threadW[i] = new Thread(() => Writer(i1 * 10000, (i1 + 1) * 10000));
                threadR[i] = new Thread(() => Reader(i1 * 10000, (i1 + 1) * 10000));
            }

            // Writers need to put stuff in the DB first
            for (var i = 0; i < threadNum; i++) threadW[i].Start();

            for (var i = 0; i < threadNum; i++) threadW[i].Join();

            // ... and now multiple readers
            for (var i = 0; i < threadNum; i++) threadR[i].Start();

            for (var i = 0; i < threadNum; i++) threadR[i].Join();
        }

        [Fact]
        public void OneWriteRead()
        {
            _database.StringSet("firstkey", 100).Should().BeTrue();
            _database.StringGet("firstkey").Should().Be(100);
        }

        [Fact]
        public void WriteByteArrayValue()
        {
            var bytes = Encoding.ASCII.GetBytes("abcdef");

            _database.StringSet("firstkey", bytes).Should().BeTrue();
            Encoding.ASCII.GetString(_database.StringGet("firstkey")).Should().Be("abcdef");
        }

        [Fact]
        public void WriteReadMany()
        {
            const int num = 15000;
            for (var i = 0; i < num; i++) _database.StringSet($"mykey:{i}", i);

            for (var i = 0; i < num; i++) _database.StringGet($"mykey:{i}").Should().Be(i);
        }
    }
}