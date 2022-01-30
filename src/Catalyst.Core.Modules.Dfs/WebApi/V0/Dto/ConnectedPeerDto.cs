#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Core.Lib.Util;
using Lib.P2P;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Dto
{
    /// <summary>
    ///   Information on a peer.
    /// </summary>
    public class ConnectedPeerDto
    {
        /// <summary>
        ///  The unique ID of the peer.
        /// </summary>
        public string Peer;

        /// <summary>
        ///   The connected address.
        /// </summary>
        public string Addr;

        /// <summary>
        ///   Avg time to the peer.
        /// </summary>
        public string Latency;

        /// <summary>
        ///   Creates a new peer info.
        /// </summary>
        public ConnectedPeerDto(Peer peer)
        {
            Peer = peer.Id.ToString();
            Addr = peer.ConnectedAddress?.WithoutPeerId().ToString();
            Latency = peer.Latency == null ? "n/a" : Duration.Stringify(peer.Latency.Value, string.Empty);
        }
    }
}
