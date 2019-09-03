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
using Catalyst.Abstractions.Dfs;
using Multiformats.Hash;

namespace Catalyst.Abstractions.Consensus.Deltas
{
    /// <summary>
    /// This service should be used by components which need to access the history of ledger state updates as
    /// they get confirmed and appear on the Dfs, it provides access to a chronology of updates, as well as a
    /// live stream of the updates.
    /// </summary>
    public interface IDeltaHashProvider
    {
        /// <summary>
        /// Call this method to try and update the latest hash on the cache. This adds the new hash in memory, and if the cache is full,
        /// also evicts the oldest hash from the cache.
        /// </summary>
        /// <param name="previousHash">The hash that is supposed to chronologically precede the new one.</param>
        /// <param name="newHash">The new and latest hash, which should replace <see cref="previousHash"/>.</param>
        /// <returns><see>
        ///         <cref>true</cref>
        ///     </see>
        ///     if the update was successful, <see>
        ///         <cref>false</cref>
        ///     </see>
        ///     otherwise.</returns>
        bool TryUpdateLatestHash(Multihash previousHash, Multihash newHash);

        /// <summary>
        /// Retrieve the latest ledger update, as seen from an optional point in time in the past.
        /// </summary>
        /// <param name="asOf">An optional point in time in the past, if not provided, it will use <see cref="IDateTimeProvider.UtcNow"/></param>
        /// <returns>The hash, or address on the DFS, of the latest ledger state update as a string, as returned in <seealso cref="IDfs.AddAsync"/></returns>
        Multihash GetLatestDeltaHash(DateTime? asOf = null);

        /// <summary>
        /// Subscribe to these updates to get live notifications for the hashes of new deltas as they get published on the Dfs.
        /// </summary>
        IObservable<Multihash> DeltaHashUpdates { get; }
    }
}
