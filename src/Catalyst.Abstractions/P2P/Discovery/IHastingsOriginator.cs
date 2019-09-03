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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Types;
using Catalyst.Protocol.IPPN;

namespace Catalyst.Abstractions.P2P.Discovery
{
    public interface IHastingsOriginator
    {
        IPeerIdentifier Peer { get; }

        /// <summary>
        /// Every time you the walk moves forward with a new Peer, it will ask that peer for
        /// its neighbours sending a new <see cref="Protocol.IPPN.PeerNeighborsRequest"/>.
        /// This field stores the details for that request. 
        /// </summary>
        ICorrelationId PnrCorrelationId { get; }
        
        /// <summary>
        /// A readonly list of the neighbours considered for <see cref="PingRequest"/> at that stage of the node.
        /// This list will be used to progress if any of them is valid (<see cref="HasValidCandidate"/>).
        /// </summary>
        INeighbours Neighbours { get; }
        
        /// <summary>
        ///     creates a memento from current state
        /// </summary>
        /// <returns></returns>
        IHastingsMemento CreateMemento();

        /// <summary>
        ///     Restores the state from a memento
        /// </summary>
        /// <param name="hastingsMemento"></param>
        void RestoreMemento(IHastingsMemento hastingsMemento);

        /// <summary>
        /// Find out if the current state has any neighbour in a <see cref="NeighbourStateTypes.Responsive"/>
        /// </summary>
        /// <returns><see cref="true"/> if <see cref="Neighbours"/> contains a <see cref="NeighbourStateTypes.Responsive"/> neighbour, <see cref="false"/> otherwise.</returns>
        bool HasValidCandidate();
    }
}
