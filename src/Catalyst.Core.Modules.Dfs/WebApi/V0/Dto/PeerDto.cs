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

using System.Collections.Generic;
using System.Linq;
using Lib.P2P;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Dto
{
    /// <summary>
    ///   Information on a peer.
    /// </summary>
    public class PeerInfoDto
    {
        /// <summary>
        ///  The unique ID of the peer.
        /// </summary>
        public string Id;

        /// <summary>
        ///   The public key of the peer.
        /// </summary>
        public string PublicKey;

        /// <summary>
        ///   The addresses that the peer is listening on.
        /// </summary>
        public IEnumerable<string> Addresses;

        /// <summary>
        ///   The version of the software.
        /// </summary>
        public string AgentVersion;

        /// <summary>
        ///   The version of the protocol.
        /// </summary>
        public string ProtocolVersion;

        /// <summary>
        ///   Creates a new peer info.
        /// </summary>
        public PeerInfoDto(Peer peer)
        {
            Id = peer.Id.ToBase58();
            PublicKey = peer.PublicKey;
            Addresses = peer.Addresses.Select(a => a.ToString());
            AgentVersion = peer.AgentVersion;
            ProtocolVersion = peer.ProtocolVersion;
        }
    }
}
