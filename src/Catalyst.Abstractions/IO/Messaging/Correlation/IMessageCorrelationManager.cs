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
using Catalyst.Protocol.Common;

namespace Catalyst.Abstractions.IO.Messaging.Correlation
{
    /// <summary>
    /// The message correlation manager is a cached user to store correlation Ids of outgoing requests
    /// and use them to match incoming responses.
    /// </summary>
    public interface IMessageCorrelationManager : IDisposable
    {
        /// <summary>
        /// Adds a correlatable message to the cache, which can then be matched in
        /// <see cref="TryMatchResponse"/> upon receiving the response.
        /// in the cache.
        /// </summary>
        /// <param name="correlatableMessage">The (outgoing) correlatable message to add to the cache.</param>
        void AddPendingRequest(ICorrelatableMessage<ProtocolMessage> correlatableMessage);

        /// <summary>
        /// Tries to match the response by checking its correlation Id is still
        /// in the cache.
        /// </summary>
        /// <param name="response">A response type message received from the network</param>
        /// <returns>
        ///     <see>
        ///         <cref>true</cref>
        ///     </see>
        ///     if the response was matched,
        ///     <see>
        ///         <cref>false</cref>
        ///     </see>
        ///     otherwise.</returns>
        bool TryMatchResponse(ProtocolMessage response);
    }
}
