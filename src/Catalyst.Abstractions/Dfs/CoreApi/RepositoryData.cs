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

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   The statistics for <see cref="IStatsApi.RepositoryAsync"/>.
    /// </summary>
    public class RepositoryData
    {
        /// <summary>
        ///   The number of blocks in the repository.
        /// </summary>
        /// <value>
        ///   The number of blocks in the <see cref="IBlockRepositoryApi">repository</see>.
        /// </value>
        public ulong NumObjects;

        /// <summary>
        ///   The total number bytes in the repository.
        /// </summary>
        /// <value>
        ///   The total number bytes in the <see cref="IBlockRepositoryApi">repository</see>.
        /// </value>
        public ulong RepoSize;

        /// <summary>
        ///   The fully qualified path to the repository.
        /// </summary>
        /// <value>
        ///   The directory of the <see cref="IBlockRepositoryApi">repository</see>.
        /// </value>
        public string RepoPath;

        /// <summary>
        ///   The version number of the repository.
        /// </summary>
        /// <value>
        ///  The version number of the <see cref="IBlockRepositoryApi">repository</see>.
        /// </value>
        public string Version;

        /// <summary>
        ///   The maximum number of bytes allowed in the repository.
        /// </summary>
        /// <value>
        ///  Max bytes allowed in the <see cref="IBlockRepositoryApi">repository</see>.
        /// </value>
        public ulong StorageMax;
    }
}
