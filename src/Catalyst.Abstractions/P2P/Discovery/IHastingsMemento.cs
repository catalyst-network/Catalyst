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

using Catalyst.Protocol.IPPN;

namespace Catalyst.Abstractions.P2P.Discovery
{
    /// <summary>
    /// A memento object used to store and restore the valid states of the Hastings walk.
    /// More information on <seealso cref="https://en.wikipedia.org/wiki/Memento_pattern"/>
    /// </summary>
    public interface IHastingsMemento
    {
        /// <summary>
        /// The peer identifier of the node used to discover new nodes.
        /// </summary>
        IPeerIdentifier Peer { get; }

        /// <summary>
        /// A list of neighbours, provided by <see cref="Peer"/> through a <see cref="PeerNeighborsResponse"/>
        /// </summary>
        INeighbours Neighbours { get; }
    }
}
