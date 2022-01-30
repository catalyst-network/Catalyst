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

using System;
using MultiFormats;

namespace Lib.P2P.Discovery
{
    /// <summary>
    ///   Describes a service that finds a peer.
    /// </summary>
    /// <remarks>
    ///   All discovery services must raise the <see cref="PeerDiscovered"/> event.
    /// </remarks>
    public interface IPeerDiscovery : IService
    {
        /// <summary>
        ///   Raised when a peer is discovered.
        /// </summary>
        /// <remarks>
        ///   The peer must contain at least one <see cref="MultiAddress"/>.
        ///   The address must end with the ipfs protocol and the public ID
        ///   of the peer.  For example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
        /// </remarks>
        event EventHandler<Peer> PeerDiscovered;
    }
}
