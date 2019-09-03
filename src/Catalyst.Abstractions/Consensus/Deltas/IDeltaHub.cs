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

using System.Threading;
using System.Threading.Tasks;
using Catalyst.Protocol.Deltas;

namespace Catalyst.Abstractions.Consensus.Deltas
{
    /// <summary>
    /// For lack of a better name, this <see cref="IDeltaHub"/> is meant to be the service used to
    /// publish new candidate deltas, favourite deltas, or publish newly elected deltas
    /// on the Dfs, etc. It is basically the outbound interface between the node and the rest of the
    /// network, through which all delta building related messages should be broadcast.
    /// </summary>
    public interface IDeltaHub
    {
        /// <summary>
        /// When the node produces a new candidate delta, it should then use this method to trigger
        /// the gossiping of the candidate through the network. This should only be called once,
        /// after a new candidate has been created, the gossiping functionality is then handled
        /// by network specific classes.
        /// </summary>
        /// <param name="candidate">The newly produced candidate.</param>
        void BroadcastCandidate(CandidateDeltaBroadcast candidate);

        /// <summary>
        /// When the election of the best delta for a given cycle ends, each producer is required to
        /// submit its favourite candidate to the rest of the participants in order for consensus to
        /// emerge. This function should be called to trigger the broadcasting of the elected best
        /// candidate delta for a given cycle.
        /// </summary>
        /// <seealso>
        ///     <cref>https://github.com/catalyst-network/Catalyst.Node/blob/develop/Documentation/PoA.md#voting-phase</cref>
        /// </seealso>
        /// <param name="favourite">The favourite delta, as produced by this node.</param>
        void BroadcastFavouriteCandidateDelta(FavouriteDeltaBroadcast favourite);

        /// <summary>
        /// Once a delta has been elected, if the node possesses the full content for the elected delta,
        /// it should then post it on the DFS (if it can't find it there already) so that the rest of the
        /// network can retrieve and apply it.
        /// </summary>
        /// <param name="delta">The delta which has been elected for this cycle.</param>
        /// <param name="cancellationToken">A cancellation token allowing to abort the tasks.</param>
        Task<string> PublishDeltaToDfsAndBroadcastAddressAsync(Delta delta, CancellationToken cancellationToken = default);
    }
}
