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
using MultiFormats;

namespace Catalyst.Abstractions.Options
{
    /// <summary>
    ///   Configuration options for discovering other peers.
    /// </summary>
    /// <seealso cref="DfsOptions"/>
    public class DiscoveryOptions
    {
        /// <summary>
        ///   Well known peers used to find other peers in
        ///   the IPFS network.
        /// </summary>
        /// <value>
        ///   The default value is <b>null</b>.
        /// </value>
        /// <remarks>
        ///   If not null, then the sequence is use by
        ///   the block API; otherwise the values in the configuration
        ///   file are used.
        /// </remarks>
        public IEnumerable<MultiAddress> BootstrapPeers { set; get; }

        /// <summary>
        ///   Disables the multicast DNS discovery of other peers
        ///   and advertising of this peer.
        /// </summary>
        public bool DisableMdns { set; get; }

        /// <summary>
        ///   Disables discovery of other peers by walking the
        ///   DHT.
        /// </summary>
        public bool DisableRandomWalk { set; get; }

        public DiscoveryOptions(){}
    }
}
