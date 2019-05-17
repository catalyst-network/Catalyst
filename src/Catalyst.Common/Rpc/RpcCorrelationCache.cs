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

using System;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Common.Rpc
{
    public sealed class RpcCorrelationCache
        : MessageCorrelationCacheBase, IRpcCorrelationCache
    {
        public RpcCorrelationCache(IMemoryCache cache,
            ILogger logger,
            TimeSpan cacheTtl = default) 
            : base(cache, logger, cacheTtl) { }

        /// <summary>
        ///     Allows base constructor to get our ChangeReputationOnEviction none static method in a static context
        ///     by passing what we need as a delegated action.
        /// </summary>
        /// <returns></returns>
        protected override PostEvictionDelegate GetInheritorDelegate() { return ChangeReputationOnEviction; }

        private void ChangeReputationOnEviction(object key, object value, EvictionReason reason, object state)
        {
            // we don't having anything to really do here for rcp clients, yet.
            Logger.Debug("RpcCorrelationCache.ChangeReputationOnEviction() called");
        }
    }
}
