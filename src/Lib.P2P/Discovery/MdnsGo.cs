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

namespace Lib.P2P.Discovery
{
    /// <summary>
    ///   Discovers peers using Multicast DNS according to
    ///   go-ipfs v0.4.17
    /// </summary>
    /// <remarks>
    ///   GO peers are not using the mDNS multicast address (224.0.0.251)
    ///   <see href="https://github.com/libp2p/go-libp2p/issues/469"/>.
    ///   Basically this cannot work until the issue is resolved.
    /// </remarks>
    public class MdnsGo : MdnsJs
    {
        /// <summary>
        ///   MDNS go is the same as MdnsJs except that the
        ///   service name is "_ipfs-discovery._udp".
        /// </summary>
        public MdnsGo() { ServiceName = "_ipfs-discovery._udp"; }
    }
}
