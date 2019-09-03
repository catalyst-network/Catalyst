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
using Catalyst.Protocol.Deltas;

namespace Catalyst.Abstractions.Consensus.Deltas
{
    /// <summary>
    /// Provides convenience method to read Delta from the Dfs.
    /// </summary>
    public interface IDeltaDfsReader
    {
        /// <summary>
        /// Asynchronously retrieves the content at the hash/address on the Dfs, and tries to parse it as a Delta.
        /// </summary>
        /// <param name="hash">The hash or address of the delta on the Dfs.</param>
        /// <param name="delta">The retrieved delta.</param>
        /// <param name="cancellationToken">An optional cancellation token which can be used to interrupt the tasks.</param>
        /// <returns><see>
        ///         <cref>true</cref>
        ///     </see>
        ///     if the retrieval was successful, <see>
        ///         <cref>false</cref>
        ///     </see>
        ///     otherwise.</returns>
        bool TryReadDeltaFromDfs(string hash, out Delta delta, CancellationToken cancellationToken = default);
    }
}
