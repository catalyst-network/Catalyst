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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Abstractions.Dfs.Migration
{
    public interface IMigrationManager
    {
        /// <summary>
        ///   The list of migrations that can be performed.
        /// </summary>
        List<IMigration> Migrations { get; }

        /// <summary>
        ///   Gets the latest supported version number of a repository.
        /// </summary>
        int LatestVersion { get; }

        /// <summary>
        ///   Gets the current vesion number of the  repository.
        /// </summary>
        int CurrentVersion { get; }

        /// <summary>
        ///   Upgrade/downgrade to the specified version.
        /// </summary>
        /// <param name="version">
        ///   The required version of the repository.
        /// </param>
        /// <param name="cancel">
        /// </param>
        /// <returns></returns>
        Task MirgrateToVersionAsync(int version, CancellationToken cancel = default(CancellationToken));  
    }
}
