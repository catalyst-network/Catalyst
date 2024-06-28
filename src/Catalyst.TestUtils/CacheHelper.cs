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
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using NSubstitute;

namespace Catalyst.TestUtils
{
    public static class CacheHelper
    {
        public static Dictionary<ByteString, ICacheEntry> MockCacheEvictionCallback(object key, IMemoryCache cache, Dictionary<ByteString, ICacheEntry> cacheEntriesByRequest)
        {
            var correlationId = (ByteString) key;
            var cacheEntry = Substitute.For<ICacheEntry>();
            var expirationTokens = new List<IChangeToken>();
            cacheEntry.ExpirationTokens.Returns(expirationTokens);
            var expirationCallbacks = new List<PostEvictionCallbackRegistration>();
            cacheEntry.PostEvictionCallbacks.Returns(expirationCallbacks);

            cache.CreateEntry(correlationId).Returns(cacheEntry);
            cacheEntriesByRequest.Add(correlationId, cacheEntry);
            return cacheEntriesByRequest;
        }
    }
}
