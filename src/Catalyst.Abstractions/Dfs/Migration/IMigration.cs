#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Catalyst.Abstractions.Options;

namespace Catalyst.Abstractions.Dfs.Migration
{
    /// <summary>
    ///   Provides a migration path to the repository.
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        ///   The repository version that is created.
        /// </summary>
        int Version { get; }

        /// <summary>
        ///   Indicates that an upgrade can be performed.
        /// </summary>
        bool CanUpgrade { get; }

        /// <summary>
        ///   Indicates that an downgrade can be performed.
        /// </summary>
        bool CanDowngrade { get; }

        /// <summary>
        ///   Upgrade the repository.
        /// </summary>
        /// <param name="ipfs">
        ///   The IPFS system to upgrade.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task UpgradeAsync(RepositoryOptions options, CancellationToken cancel = default);

        /// <summary>
        ///   Downgrade the repository.
        /// </summary>
        /// <param name="ipfs">
        ///   The IPFS system to downgrade.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task DowngradeAsync(RepositoryOptions options, CancellationToken cancel = default);
    }
}
