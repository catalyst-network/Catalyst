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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.P2P.Repository;

namespace Catalyst.Core.Consensus.Deltas
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
        /// <param name="previousDeltaHash">The content based address of the previous delta on the Dfs.</param>
        /// <returns>The list of peers which are eligible for the production of the delta following <see cref="previousDeltaHash"/></returns>
        IList<IPeerIdentifier> GetDeltaProducersFromPreviousDelta(byte[] previousDeltaHash);

        /// <summary>
        /// A peer repository containing peers eligible for the production of the next delta.
        /// </summary>
        IPeerRepository PeerRepository { get; }
    }
}
