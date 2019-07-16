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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Protocol.Deltas;

namespace Catalyst.Common.Interfaces.Modules.Consensus.Deltas
{
    /// <summary>
    /// For lack of a better name, this <see cref="IDeltaHub"/> is meant to be the service used to
    /// publish new candidate deltas, receive incoming ones, get notified about the election of a
    /// new delta on the Dfs, etc. It is basically the interface between the node and the rest of the
    /// network, through which all delta related messages should be passed.
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
        /// This method should be called in order for the hub to start listening for incoming candidate
        /// deltas produced by the rest of the network. Voting, amongst other processes will be kicked off
        /// by calling this method.
        /// </summary>
        /// <param name="candidateStream">The stream on which the incoming candidate deltas are expected
        /// to be pushed.</param>
        void SubscribeToCandidateStream(IObservable<CandidateDeltaBroadcast> candidateStream);

        /// <summary>
        /// When the election of the best delta for a given cycle ends, each producer is required to
        /// submit its favourite candidate to the rest of the participants in order for consensus to
        /// emerge. This function should be called to trigger the broadcasting of the elected best
        /// candidate delta for a given cycle.
        /// </summary>
        /// <seealso>
        ///     <cref>https://github.com/catalyst-network/Catalyst.Node/blob/develop/Documentation/PoA.md#voting-phase</cref>
        /// </seealso>
        /// <param name="previousDeltaDfsHash">The hash of the delta preceding the candidate we favor.
        /// This is basically as an identifier for the cycle we are currently sending our vote for.</param>
        void BroadcastFavouriteCandidateDelta(byte[] previousDeltaDfsHash);

        /// <summary>
        /// This method should be called in order for the hub to start listening for incoming vote results
        /// at the end of the voting phase of each cycle. Receiving these favourite deltas will allow it to
        /// choose which hash is the best for the cycle and potentially publish it or look for it on IPFS.
        /// </summary>
        /// <seealso>
        ///     <cref>https://github.com/catalyst-network/Catalyst.Node/blob/develop/Documentation/PoA.md#voting-phase</cref>
        /// </seealso>
        /// <param name="favouriteCandidateStream">The stream on which the incoming favourite deltas votes are
        /// expected to be pushed.</param>
        void SubscribeToFavouriteCandidateStream(IObservable<FavouriteDeltaBroadcast> favouriteCandidateStream);

        /// <summary>
        /// Once a delta has been elected, if the node possesses the full content for the elected delta,
        /// it should then post it on IPFS (if it can't find it there already) so that the rest of the
        /// network can retrieve and apply it.
        /// </summary>
        /// <param name="delta">The delta which has been elected for this cycle.</param>
        /// <param name="cancellationToken">A cancellation token allowing to abort the tasks.</param>
        Task<string> PublishDeltaToIpfsAsync(Protocol.Deltas.Delta delta, CancellationToken cancellationToken = default);

        /// <summary>
        /// This method should be called in order for the hub to start listening for incoming deltas on the
        /// DFS at the end of each cycle. If the received hash is unknown, the new delta has not been seen
        /// by the node and should be fetched from IPFS.
        /// </summary>
        /// <param name="dfsDeltaAddressStream">The stream on which the addresses of new deltas on
        /// the DFS should be pushed.</param>
        void SubscribeToDfsDeltaStream(IObservable<byte[]> dfsDeltaAddressStream);
    }
}
