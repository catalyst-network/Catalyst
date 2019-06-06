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
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Protocol.Common;

namespace Catalyst.Common.Interfaces.P2P
{
    public interface IPeerService : IChanneledMessageStreamer<ProtocolMessage>, IDisposable
    {
        /// <summary>
        ///     The discovery mechanism for the peer network.
        /// </summary>
        IPeerDiscovery Discovery { get; }
                
        // /// <summary>
        // ///     Request the node at <see cref="targetNode" /> for a list of peers.
        // /// </summary>
        // /// <param name="queryingNode">Identifier of the node making the request.</param>
        // /// <param name="targetNode">Identifier of the node supposed to reply to the request with a list of peers.</param>
        // /// <returns></returns>
        // List<IPeerIdentifier> FindNode(IPeerIdentifier queryingNode, IPeerIdentifier targetNode);
        //
        // /// <summary>
        // /// </summary>
        // /// <returns></returns>
        // List<IPeerIdentifier> GetPeers(IPeerIdentifier queryingNode);
        //
        // /// <summary>
        // /// </summary>
        // /// <param name="k"></param>
        // /// <param name="v"></param>
        // /// <returns></returns>
        // bool Store(string k, byte[] v);
        //
        // /// <summary>
        // ///     If a corresponding value is present on the queried node, the associated data is returned.
        // ///     Otherwise the return value is the return equivalent to FindNode()
        // /// </summary>
        // /// <param name="k"></param>
        // /// <returns></returns>
        // dynamic FindValue(string k);
        //
        // /// <summary>
        // ///     Reflects back current nodes peer bucket
        // /// </summary>
        // /// <param name="queryingNode"></param>
        // /// <returns></returns>
        // List<IPeerIdentifier> PeerExchange(IPeerIdentifier queryingNode);
    }
}
