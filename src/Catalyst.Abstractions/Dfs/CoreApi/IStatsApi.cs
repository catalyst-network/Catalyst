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
using Lib.P2P.Transports;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Get statistics/diagnostics for the various core components.
    /// </summary>
    public interface IStatsApi
    {
        /// <summary>
        ///   Get statistics on network bandwidth.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the current <see cref="BandwidthData"/>.
        /// </returns>
        /// <seealso cref="ISwarmApi"/>
        Task<BandwidthData> GetBandwidthStatsAsync(CancellationToken cancel = default);

        /// <summary>
        ///   Get statistics on the blocks exchanged with other peers.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the current <see cref="BitswapData"/>.
        /// </returns>
        /// <seealso cref="IBitSwapApi"/>
        BitswapData GetBitSwapStats(CancellationToken cancel = default);

        /// <summary>
        ///   Get statistics on the repository.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the current <see cref="RepositoryData"/>.
        /// </returns>
        /// <remarks>
        ///   Same as <see cref="IBlockRepositoryApi.StatisticsAsync(CancellationToken)"/>.
        /// </remarks>
        /// <seealso cref="IBlockRepositoryApi"/>
        Task<RepositoryData> GetRepositoryStatsAsync(CancellationToken cancel = default);
    }
}
