#region LICENSE

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

#endregion

using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using NSubstitute;

namespace Catalyst.TestUtils
{
    public static class CacheHelper
    {
        public static MockedCache GetMockedCache()
        {
            return new MockedCache();
        }

        public sealed class MockedCache
        {
            private readonly IMemoryCache _memoryCache;
            public readonly IDictionary<ByteString, ICacheEntry> _cacheEntries;
            private readonly List<CorrelatableMessage<ProtocolMessage>> _correlatableMessages;

            internal MockedCache(IDictionary<ByteString, ICacheEntry> cacheEntries = default, IMemoryCache memoryCache = default)
            {
                _cacheEntries = cacheEntries ?? new Dictionary<ByteString, ICacheEntry>();
                _memoryCache = memoryCache ?? Substitute.For<IMemoryCache>();
            }
            
            /// <summary>
            ///      Registers mocked eviction call back behavior on cache entry.
            /// </summary>
            /// <param name="key"></param>
            public void MockCacheEvictionCallback(object key)
            {
                var correlationId = (ByteString) key;
                var cacheEntry = Substitute.For<ICacheEntry>();
                cacheEntry.ExpirationTokens.Returns(new List<IChangeToken>());
                var expirationCallbacks = new List<PostEvictionCallbackRegistration>();
                cacheEntry.PostEvictionCallbacks.Returns(expirationCallbacks);

                _memoryCache.CreateEntry(correlationId).Returns(cacheEntry);
                _cacheEntries.Add(correlationId, cacheEntry);
            }
        }
    }
}
