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
using Lib.P2P.Protocols;

namespace Lib.P2P.Routing 
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDhtService : IPeerProtocol, IService, IPeerRouting, IContentRouting
    {
        /// <summary>
        ///   Provides access to other peers.
        /// </summary>
        ISwarmService SwarmService { get; set; }

        RoutingTable RoutingTable { get; }

        /// <summary>
        ///   The number of closer peers to return.
        /// </summary>
        /// <value>
        ///   Defaults to 20.
        /// </value>
        int CloserPeerCount { get; set; }

        /// <summary>
        ///   Raised when the DHT is stopped.
        /// </summary>
        /// <seealso cref="DhtService.StopAsync"/>
        event EventHandler Stopped;

        /// <inheritdoc />
        string ToString();

        /// <summary>
        ///   Advertise that we can provide the CID to the X closest peers
        ///   of the CID.
        /// </summary>
        /// <param name="cid">
        ///   The CID to advertise.ipfs
        /// </param>
        /// <remarks>
        ///   This starts a background process to send the AddProvider message
        ///   to the 4 closest peers to the <paramref name="cid"/>.
        /// </remarks>
        void Advertise(Cid cid);

        /// <summary>
        ///   Process a find node request.
        /// </summary>
        DhtMessage ProcessFindNode(DhtMessage request, DhtMessage response);

        /// <summary>
        ///   Process a get provider request.
        /// </summary>
        DhtMessage ProcessGetProviders(DhtMessage request, DhtMessage response);

        /// <summary>
        ///   Process an add provider request.
        /// </summary>
        DhtMessage ProcessAddProvider(Peer remotePeer, DhtMessage request, DhtMessage response);
    }
}
