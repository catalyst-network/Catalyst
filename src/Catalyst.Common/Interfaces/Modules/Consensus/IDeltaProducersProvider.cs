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
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Delta;

namespace Catalyst.Common.Interfaces.Modules.Consensus
{
    /// <summary>
    /// This is the service in charge of providing the list of PeerIdentifiers that are eligible for the
    /// production of the next delta.
    /// </summary>
    public interface IDeltaProducersProvider
    {
        /// <summary>
        /// Finds the identifiers of the peers which are allowed to produce the next delta.
        /// </summary>
        /// <param name="previousDelta">The details of the delta preceding the one for which the method is being called.</param>
        /// <returns>The list of peers which are eligible for the production of the delta following <see cref="previousDelta"/></returns>
        IList<IPeerIdentifier> GetDeltaProducersFromPreviousDelta(Delta previousDelta);

        /// <summary>
        /// The PeerDiscovery service used to collect information about the different participants on the network. This is important,
        /// for instance, when trying to find which peers should produce the next ledger state update.
        /// </summary>
        IPeerDiscovery PeerDiscovery { get; }
    }
}
