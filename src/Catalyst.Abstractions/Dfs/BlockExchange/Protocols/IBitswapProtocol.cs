#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.Protocols;

namespace Catalyst.Abstractions.Dfs.BlockExchange.Protocols
{
    /// <summary>
    ///   Features of a bitswap protocol.
    /// </summary>
    public interface IBitswapProtocol : IPeerProtocol
    {
        /// <summary>
        ///   Send a want list.
        /// </summary>
        /// <param name="stream">
        ///   The destination of the want list.
        /// </param>
        /// <param name="wants">
        ///   A sequence of <see cref="WantedBlock"/>.
        /// </param>
        /// <param name="full">
        ///   <b>true</b> if <paramref name="wants"/> is the full want list.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task SendWantsAsync(Stream stream,
            IEnumerable<WantedBlock> wants,
            bool full = true,
            CancellationToken cancel = default);
    }
}
