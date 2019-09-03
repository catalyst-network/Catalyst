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

using Catalyst.Protocol.Deltas;

namespace Catalyst.Abstractions.Consensus.Deltas
{
    /// <summary>
    /// The service in charge of building the delta state update used to update the ledger update 
    /// for a given cycle.
    /// </summary>
    public interface IDeltaBuilder
    {
        /// <summary>
        /// Builds a new candidate delta based on the content of its predecessor, and cache the full content
        /// of the delta locally. If the delta is elected at the end of the cycle, the cache will be used to retrieve
        /// and publish the whole delta onto the DFS.
        /// </summary>
        /// <param name="previousDeltaHash">The content based address of the previous delta on the Dfs.</param>
        /// <returns>Returns a candidate delta object that contains the hash for the update,
        /// the hash for the previous delta and the producer's PeerId</returns>
        CandidateDeltaBroadcast BuildCandidateDelta(byte[] previousDeltaHash);
    }
}
