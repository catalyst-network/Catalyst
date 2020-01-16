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

using Catalyst.Abstractions.Dfs;
using Lib.P2P;

namespace Catalyst.Core.Modules.Dfs.UnixFileSystem
{
    /// <summary>
    ///   A link to another <see cref="UnixFsNode"/> in the IPFS Unix File System.
    /// </summary>
    public class UnixFsLink : IFileSystemLink
    {
        /// <summary>
        ///  An empty set of links.
        /// </summary>
        public static readonly UnixFsLink[] None = new UnixFsLink[0];

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public Cid Id { get; set; }

        /// <inheritdoc />
        public long Size { get; set; }
    }
}
