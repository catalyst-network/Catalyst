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

using System.Linq;
using System.Reflection;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Config;
using Catalyst.Core.IO.Messaging.Correlation;
using Serilog;

namespace Catalyst.Core.P2P.Discovery.Hastings
{
    public sealed class HastingsOriginator : IHastingsOriginator
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        public INeighbours Neighbours { get; private set; }
        public ICorrelationId PnrCorrelationId { get; private set; }

        public IPeerIdentifier Peer { get; private set; }

        public static readonly HastingsOriginator Default = new HastingsOriginator(default);

        public HastingsOriginator(IHastingsMemento hastingsMemento)
        {
            PnrCorrelationId = CorrelationId.GenerateCorrelationId();
            Peer = hastingsMemento?.Peer;
            Neighbours = hastingsMemento?.Neighbours ?? new Neighbours();
        }

        /// <inheritdoc />
        public IHastingsMemento CreateMemento()
        {
            var worthyNeighbours = Neighbours.Where(n => n.StateTypes != NeighbourStateTypes.UnResponsive).ToList();

            Logger.Debug("Creating new memento with Peer {peer} and neighbours [{neighbours}]", 
                Peer, string.Join(", ", worthyNeighbours));
            return new HastingsMemento(Peer, new Neighbours(worthyNeighbours));
        }
        
        /// <inheritdoc />
        public void RestoreMemento(IHastingsMemento hastingsMemento)
        {
            Logger.Debug("Restoring memento with Peer {peer} and neighbours [{neighbours}]", hastingsMemento.Peer);
            Peer = hastingsMemento.Peer;
            Neighbours = hastingsMemento.Neighbours;
            PnrCorrelationId = CorrelationId.GenerateCorrelationId();
        }

        public bool HasValidCandidate()
        {
            if (Neighbours
               .Select(n => n.StateTypes)
               .Count(s => s == NeighbourStateTypes.NotContacted || s == NeighbourStateTypes.UnResponsive)
               .Equals(Constants.AngryPirate))
            {
                return false;
            }

            // see if sum of unreachable peers and reachable peers equals the total contacted number.
            return Neighbours
               .Select(n => n.StateTypes)
               .Count(s => s == NeighbourStateTypes.Responsive || s == NeighbourStateTypes.UnResponsive)
               .Equals(Constants.AngryPirate);
        }
    }
}
