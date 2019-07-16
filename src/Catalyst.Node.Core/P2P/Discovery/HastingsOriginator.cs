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

using System.Collections.Concurrent;
using System.Threading;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;

namespace Catalyst.Node.Core.P2P.Discovery
{
    public sealed class HastingsOriginator : IHastingsOriginator
    {
        private int _unreachableNeighbour;
        
        public IPeerIdentifier Peer { get; set; }
        public int ExpectedResponses { get; set; }
        public ConcurrentBag<IPeerIdentifier> CurrentPeersNeighbours { get; set; }
        
        public int UnreachableNeighbour
        {
            get => _unreachableNeighbour;
            private set => _unreachableNeighbour = value;
        }

        public HastingsOriginator()
        {
            ExpectedResponses = 0;
            _unreachableNeighbour = 0;
            CurrentPeersNeighbours = new ConcurrentBag<IPeerIdentifier>();
        }

        /// <inheritdoc />
        public IHastingMemento CreateMemento()
        {
            return new HastingMemento(Peer, CurrentPeersNeighbours);
        }
        
        /// <inheritdoc />
        public void SetMemento(IHastingMemento hastingMemento)
        {
            ExpectedResponses = 0;
            UnreachableNeighbour = 0;
            Peer = hastingMemento.Peer;
            CurrentPeersNeighbours = new ConcurrentBag<IPeerIdentifier>(hastingMemento.Neighbours);
        }

        public void IncrementUnreachablePeer()
        {
            Interlocked.Increment(ref _unreachableNeighbour);
        }
    }
}
