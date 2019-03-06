/*
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
* 
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Core.Components.Redis;
using FluentAssertions;
using StackExchange.Redis;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Helpers.Redis
{
    public class RedisConnectorTests : IDisposable
    {
        public RedisConnectorTests()
        {
            _connector = new RedisConnector(EndPoint.ToString());
            _database = _connector.Database;
        }

        public void Dispose() { _connector.Dispose(); }

        private static readonly IPEndPoint EndPoint = EndpointBuilder.BuildNewEndPoint("127.0.0.1", 6379);

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

        [Fact(Skip = "This is an integration test which relies on having a running Redis local instance.")]
        public void Connection_Should_Have_One_Endpoint()
        {
            var endpoint = _connector.Connection.GetEndPoints();
            endpoint.Length.Should().Be(1);
        }

        [Fact(Skip = "This is an integration test which relies on having a running Redis local instance.")]
        public void KeyAlreadyExistUpdate()
        {
            _database.StringSet("mykey", 100).Should().BeTrue();
            _database.StringGet("mykey").Should().Be(100);
            _database.StringSet("mykey", 200).Should().BeTrue();
            _database.StringGet("mykey").Should().Be(200);
        }

        [Fact(Skip = "This is an integration test which relies on having a running Redis local instance.")]
        public void KeyDoesNotExists()
        {
            var randomUnknownKey = Guid.NewGuid().ToString();
            var ret = _database.StringGet(randomUnknownKey);
            ret.HasValue.Should().BeFalse("this random key should never have been inserted in Redis before.");
        }

        [Fact(Skip = "This is an integration test which relies on having a running Redis local instance.")]
        public async Task KeysArePersistent()
        {
            var key = "persisted_key";
            var value = "persisted_value";
            await _database.KeyDeleteAsync(key);
            (await _database.StringGetAsync(key)).HasValue.Should().BeFalse("we just deleted that key");

            await _database.StringSetAsync(key, value);

            await Task.Delay(500); // after 500ms the changes is saved

            var redisValue = await _database.StringGetAsync(key);
            redisValue.HasValue.Should().BeTrue();
            redisValue.Should().Be(value);

            await _connector.Connection.CloseAsync();
            _connector.Connection.IsConnected.Should().BeFalse();
            _connector.Dispose();
            await Task.Delay(500);

            using (var newConnector = new RedisConnector(EndPoint.ToString()))
            {
                var database = newConnector.Database;
                var persistedValue = await database.StringGetAsync(key);
                persistedValue.HasValue.Should().BeTrue();
                persistedValue.Should().Be(value);

                //do some cleaning
                await database.KeyDeleteAsync(key);
            }
        }

        [Fact(Skip = "This is an integration test which relies on having a running Redis local instance.")]
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

        [Fact(Skip = "This is an integration test which relies on having a running Redis local instance.")]
        public void OneWriteRead()
        {
            _database.StringSet("firstkey", 100).Should().BeTrue();
            _database.StringGet("firstkey").Should().Be(100);
        }

        [Fact(Skip = "This is an integration test which relies on having a running Redis local instance.")]
        public void WriteByteArrayValue()
        {
            var bytes = Encoding.ASCII.GetBytes("abcdef");

            _database.StringSet("firstkey", bytes).Should().BeTrue();
            Encoding.ASCII.GetString(_database.StringGet("firstkey")).Should().Be("abcdef");
        }

        [Fact(Skip = "This is an integration test which relies on having a running Redis local instance.")]
        public void WriteReadMany()
        {
            const int num = 15000;
            for (var i = 0; i < num; i++) _database.StringSet($"mykey:{i}", i);

            for (var i = 0; i < num; i++) _database.StringGet($"mykey:{i}").Should().Be(i);
        }
    }
}