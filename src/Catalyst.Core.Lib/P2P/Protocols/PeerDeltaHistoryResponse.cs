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
using Catalyst.Abstractions.P2P.Protocols;
using Catalyst.Protocol.Peer;
using Dawn;
using LibP2P;

namespace Catalyst.Core.Lib.P2P.Protocols
{
    public class PeerDeltaHistoryResponse : ProtocolResponseBase, IPeerDeltaHistoryResponse
    {
        public IEnumerable<Cid> DeltaCid { get; }

        public PeerDeltaHistoryResponse(PeerId peerId, IEnumerable<Cid> deltaCid) : base(peerId)
        {
            var enumerable = deltaCid as Cid[] ?? deltaCid.ToArray();
            Guard.Argument(enumerable.Length).InRange(1, 1024);
            DeltaCid = enumerable;
        }
    }
}
