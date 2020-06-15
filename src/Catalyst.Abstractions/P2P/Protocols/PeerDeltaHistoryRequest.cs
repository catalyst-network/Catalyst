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
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MultiFormats;

namespace Catalyst.Abstractions.P2P.Protocols
{
    /// <summary>
    /// Protocol of requesting index of delta histories from peers
    /// </summary>
    public interface IPeerDeltaHistoryRequest : IProtocolRequest, IDisposable
    {
        ReplaySubject<IPeerDeltaHistoryResponse> DeltaHistoryResponseMessageStreamer { get; }

        /// <summary>
        ///     Request an index of delta cid from a peer at a given height.
        /// </summary>
        /// <param name="recipientPeerIdentifier"> The recipient peer identifier </param>
        /// <param name="height"> Delta height to request </param>
        /// <param name="range"> number of deltas requested </param>
        Task<IPeerDeltaHistoryResponse> DeltaHistoryAsync(MultiAddress recipientPeerIdentifier, uint height, uint range);
    }
}
