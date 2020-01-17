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

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Manages all the blocks stored locally.
    /// </summary>
    /// <seealso cref="IBlockApi"/>
    public interface IBlockRepositoryApi
    {
        /// <summary>
        ///   Perform a garbage collection sweep on the repo.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   TODO: not sure what this should return.
        /// </returns>
        Task RemoveGarbageAsync(CancellationToken cancel = default);

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
        ///   Same as <see cref="IStatsApi.RepositoryAsync(CancellationToken)"/>.
        /// </remarks>
        Task<RepositoryData> StatisticsAsync(CancellationToken cancel = default);

        /// <summary>
        ///   Verify all blocks in repo are not corrupted.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   TODO: not sure what this should return.
        /// </returns>
        Task VerifyAsync(CancellationToken cancel = default);

        /// <summary>
        ///   Gets the version number of the repo.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the version number of the data block repository.
        /// </returns>
        Task<string> VersionAsync(CancellationToken cancel = default);
    }
}
