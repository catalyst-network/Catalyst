/**
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
using System.Collections.Generic;
using System.Net;
using Dawn;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Newtonsoft;

namespace Catalyst.Node.Core.Components.Redis
{
    public class RedisStore : IRedisStore
    {
        private readonly When _when;
        private IRedisConnector _redisConnector;

        public RedisStore(When when = When.NotExists) { _when = when; }

        public RedisStore(string when)
        {
            Guard.Argument(when, nameof(when)).NotNull().NotEmpty().NotWhiteSpace();
            if (!Enum.TryParse(when, out _when))
            {
                throw new ArgumentException($"Invalid When setting format:{when}");
            }
        }

        public void Connect(IPEndPoint endPoint)
        {
            Guard.Argument(endPoint, nameof(endPoint)).NotNull();
            _redisConnector = new RedisConnector(endPoint.ToString());
        }

        ///<inheritdoc />
        public bool Set(byte[] key, byte[] value, TimeSpan? expiry)
        {
            Guard.Argument(key, nameof(key)).NotEmpty();
            Guard.Argument(value, nameof(value)).NotEmpty();
            return _redisConnector.Database.StringSet(key, value, expiry, _when);
        }

        ///<inheritdoc />
        public byte[] Get(byte[] value)
        {
            Guard.Argument(value, nameof(value)).NotEmpty();
            return _redisConnector.Database.StringGet(value);
        }

        public IDictionary<byte[], byte[]> GetSnapshot()
        {
            throw new NotImplementedException("On a big table that might require lots of resources.");
        }

        public IDictionary<string, string> GetInfo()
        {
            var serializer = new NewtonsoftSerializer();
            var sut = new StackExchangeRedisCacheClient(_redisConnector.Connection, serializer);

            return sut.GetInfo();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _redisConnector?.Dispose();
            }
        }
    }
}
