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
    /// This service should be used to retrieve and cache Delta from the Dfs
    /// </summary>
    public interface IDeltaCache
    {
        /// <summary>
        /// Attempts to retrieve a delta which has already been confirmed by other producers
        /// from the local cache first, then, if the delta was not found there,
        /// the retrieval is done from the Dfs.
        /// </summary>
        /// <param name="hash">The hash or address of the delta on the Dfs.</param>
        /// <param name="delta">The delta retrieved on the Dfs.</param>
        /// <returns><c>true</c> if the retrieval was successful, <c>false</c> otherwise.</returns>
        bool TryGetConfirmedDelta(string hash, out Delta delta);

        /// <summary>
        /// Attempts to retrieve a local delta which was locally produced and stored in
        /// cache in case it was elected in a later phase of the cycle. If the delta is not found
        /// it is because the confirmed delta was not locally produced.
        /// </summary>
        /// <param name="candidate">The candidate for which we want too retrieve the full content.</param>
        /// <param name="delta">The full delta content retrieved expected to be found on cache.</param>
        /// <returns><c>true</c> if the retrieval was successful, <c>false</c> otherwise.</returns>
        bool TryGetLocalDelta(CandidateDeltaBroadcast candidate, out Delta delta);

        /// <summary>
        /// Stores a locally produced delta, with Hash of the corresponding candidate as a key.
        /// This allows for later retrieval of the full content of the delta if the candidate gets
        /// confirmed.
        /// </summary>
        /// <param name="localCandidate">The candidate produced locally (<see cref="IDeltaBuilder.BuildCandidateDelta(byte[])"/>>)</param>
        /// <param name="delta">The full content of the produced delta.</param>
        void AddLocalDelta(CandidateDeltaBroadcast localCandidate, Delta delta);

        /// <summary>
        /// Dfs address of the content for the very first delta.
        /// </summary>
        string GenesisAddress { get; }
    }
}
