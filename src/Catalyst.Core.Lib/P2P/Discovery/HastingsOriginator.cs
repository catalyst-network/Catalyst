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
using System.Linq;
using System.Reflection;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.P2P.Discovery;
using Serilog;

namespace Catalyst.Core.Lib.P2P.Discovery
{
    public sealed class HastingsOriginator : IHastingsOriginator
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        private IPeerIdentifier _peer;
        public INeighbours Neighbours { get; private set; }
        public KeyValuePair<ICorrelationId, IPeerIdentifier> ExpectedPnr { get; private set; }

        /// <summary>
        ///     if setting a new peer, clean counters
        /// </summary>
        public IPeerIdentifier Peer
        {
            get => _peer;
            set
            {
                if (_peer != null)
                {
                    Neighbours = new Neighbours();
                    ExpectedPnr = new KeyValuePair<ICorrelationId, IPeerIdentifier>();
                }
                
                _peer = value;
            }
        }

        public HastingsOriginator(IPeerIdentifier peer, KeyValuePair<ICorrelationId, IPeerIdentifier> expectedPnr, INeighbours neighbours)
        {
            _peer = Peer;
            ExpectedPnr = expectedPnr;
            Neighbours = neighbours;
        }

        /// <inheritdoc />
        public IHastingMemento CreateMemento()
        {
            Logger.Debug("Creating new memento with Peer {peer} and neighbours [{neighbours}]", 
                Peer, string.Join(", ", Neighbours.Select(n => n.PeerIdentifier)));
            return new HastingMemento(Peer, Neighbours);
        }
        
        /// <inheritdoc />
        public void RestoreMemento(IHastingMemento hastingMemento)
        {
            Logger.Debug("Restoring memento with Peer {peer} and neighbours [{neighbours}]", hastingMemento.Peer);
            Peer = hastingMemento.Peer;
            Neighbours = hastingMemento.Neighbours;
        }
    }
}
