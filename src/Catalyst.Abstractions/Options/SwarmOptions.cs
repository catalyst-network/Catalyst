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

using Lib.P2P;
using Lib.P2P.Cryptography;

namespace Catalyst.Abstractions.Options
{
    /// <summary>
    ///   Configuration options for communication with other peers.
    /// </summary>
    /// <seealso cref="DfsOptions"/>
    public class SwarmOptions
    {
        /// <summary>
        ///   The key of the private network.
        /// </summary>
        /// <value>
        ///   The key must either <b>null</b> or 32 bytes (256 bits) in length.
        /// </value>
        /// <remarks>
        ///   When null, the public network is used.  Otherwise, the network is
        ///   considered private and only peers with the same key will be
        ///   communicated with.
        ///   <para>
        ///   When using a private network, the <see cref="DiscoveryOptions.BootstrapPeers"/>
        ///   must also use this key.
        ///   </para>
        /// </remarks>
        /// <seealso href="https://github.com/libp2p/specs/blob/master/pnet/Private-Networks-PSK-V1.md"/>
        public PreSharedKey PrivateNetworkKey { get; set; }

        /// <summary>
        ///   The low water mark for peer connections.
        /// </summary>
        /// <value>
        ///   Defaults to 0.
        /// </value>
        /// <remarks>
        ///   The <see cref="AutoDialer"/> is used to maintain at
        ///   least this number of connections.
        ///   <para>
        ///   This is an opt-feature.  The value must be positive to enable it.
        ///   </para>
        /// </remarks>
        public int MinConnections { get; set; } = 8;
    }
}
