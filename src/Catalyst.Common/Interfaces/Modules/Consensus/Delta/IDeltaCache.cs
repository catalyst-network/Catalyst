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

namespace Catalyst.Common.Interfaces.Modules.Consensus.Delta
{
    /// <summary>
    /// This service should be used to retrieve and cache Delta from the Dfs
    /// </summary>
    public interface IDeltaCache
    {
        /// <summary>
        /// Attempts to retrieve a delta from the local cache first, then, if the delta was not found there,
        /// the retrieval is done from the Dfs.
        /// </summary>
        /// <param name="hash">The hash or address of the delta on the Dfs.</param>
        /// <param name="delta">The delta retrieved on the Dfs.</param>
        /// <returns><see cref="true" /> if the retrieval was successful, <see cref="false" /> otherwise.</returns>
        bool TryGetDelta(string hash, out Protocol.Delta.Delta delta);
    }
}
