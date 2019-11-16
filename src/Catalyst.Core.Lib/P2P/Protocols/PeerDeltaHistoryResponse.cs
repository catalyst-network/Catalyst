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
using Catalyst.Abstractions.P2P.Protocols;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Peer;
using Dawn;
using Google.Protobuf;

namespace Catalyst.Core.Lib.P2P.Protocols
{
    public sealed class PeerDeltaHistoryResponse : ProtocolResponseBase, IPeerDeltaHistoryResponse
    {
        public ICollection<DeltaIndex> DeltaCid { get; }

        /// <summary>
        ///     @TODO look at side effects/ how to handle out of range index more detail
        ///     Since the protocol is over udp we need to make sure we dont fragment udp packets
        ///     Could build a buffer into the udp pipeline
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="deltaCid"></param>
        public PeerDeltaHistoryResponse(PeerId peerId, ICollection<DeltaIndex> deltaCid) : base(peerId)
        {
            Guard.Argument(deltaCid.Count).InRange(1, 1024);
            DeltaCid = deltaCid;
        }
    }
}
