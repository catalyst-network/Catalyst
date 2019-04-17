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
using Catalyst.Node.Common.Helpers.IO.Outbound;
using Catalyst.Protocol.Common;
using Google.Protobuf;

namespace Catalyst.Node.Common.Interfaces.P2P.Messaging
{
    public interface IMessageCorrelationCache : IDisposable
    {
        /// <summary>
        /// TimeSpan after which requests automatically get deleted from the cache (inflicting
        /// a reputation penalty for the peer who didn't reply).
        /// </summary>
        TimeSpan CacheTtl { get; }

        /// <summary>
        /// Tries to match a given message to a request in from the cache.
        /// </summary>
        /// <typeparam name="TRequest">The (CLR) type of the request for which the response is appropriate.</typeparam>
        /// <typeparam name="TResponse">The (CLR) type of the response we are receiving.</typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        TRequest TryMatchResponse<TRequest, TResponse>(AnySigned response)
            where TRequest : class, IMessage<TRequest>
            where TResponse : class, IMessage<TResponse>;

        void AddPendingRequest(PendingRequest pendingRequest);
    }
}
