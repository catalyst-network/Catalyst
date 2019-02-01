using System.Text;
using System.Threading;
using Catalyst.Node.Core.Components.Redis;
using Catalyst.Node.Core.Helpers.Network;
using FluentAssertions;
using StackExchange.Redis;
using Xunit;

namespace Catalyst.Node.UnitTests.Helpers.Redis
{
    public class UT_RedisConnector
    {
        private static readonly ConnectionMultiplexer Cm = RedisConnector
            .GetInstance(EndpointBuilder.BuildNewEndPoint("127.0.0.1", 6379)).Connection;

        private static readonly IDatabase Db = RedisConnector
            .GetInstance(EndpointBuilder.BuildNewEndPoint("127.0.0.1", 6379)).GetDb;

        private static void Writer(int start, int end)
        {
            for (var k = start; k < end; k++) Db.StringSet($"mykey:{k}", k).Should().BeTrue();
        }

        private static void Reader(int start, int end)
        {
            for (var k = start; k < end; k++) Db.StringGet($"mykey:{k}").Should().Be(k);
        }

        public UT_RedisConnector()
        {
            var endpoint = Cm.GetEndPoints();
            endpoint.Length.Should().Be(1);

            var server = Cm.GetServer(endpoint[0]);
            server.FlushDatabase(); // clean up before each test
        }


        [Fact]
        public void OneWriteRead()
        {
            Db.StringSet("firstkey", 100).Should().BeTrue();
            Db.StringGet("firstkey").Should().Be(100);
        }

        [Fact]
        public void WriteByteArrayValue()
        {
            var bytes = Encoding.ASCII.GetBytes("abcdef");

            Db.StringSet("firstkey", bytes).Should().BeTrue();
            Encoding.ASCII.GetString(Db.StringGet("firstkey")).Should().Be("abcdef");
        }

        [Fact]
        public void KeyDoesNotExists()
        {
            var ret = Db.StringGet("mykey");
            ret.HasValue.Should().BeFalse("Expected no value with this key in Redis");
        }

        [Fact]
        public void KeyAlreadyExistUpdate()
        {
            Db.StringSet("mykey", 100).Should().BeTrue();
            Db.StringGet("mykey").Should().Be(100);
            Db.StringSet("mykey", 200).Should().BeTrue();
            Db.StringGet("mykey").Should().Be(200);
        }

        [Fact]
        public void WriteReadMany()
        {
            const int num = 15000;
            for (var i = 0; i < num; i++) Db.StringSet($"mykey:{i}", i);

            for (var i = 0; i < num; i++) Db.StringGet($"mykey:{i}").Should().Be(i);
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
    }
}