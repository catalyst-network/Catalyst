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

using System;
using System.Collections.Generic;
using System.IO;
using Catalyst.Abstractions.Dfs;
using Lib.P2P;

namespace Catalyst.Core.Modules.Dfs.UnixFs
{
    /// <summary>
    ///   A node in the IPFS Unix File System.
    /// </summary>
    /// <remarks>
    ///   A <b>FileSystemNode</b> is either a directory or a file
    ///   <para>
    ///   A directory's <see cref="Links"/> is a sequence of files/directories
    ///   belonging to the directory.
    ///   </para>
    /// </remarks>
    public class UnixFsNode : IFileSystemNode
    {
        /// <inheritdoc />
        public bool IsDirectory { get; set; }

        /// <inheritdoc />
        public IEnumerable<IFileSystemLink> Links { get; set; }

        /// <inheritdoc />
        public byte[] DataBytes { get; set; }

        /// <inheritdoc />
        public Stream DataStream { get; set; }

        /// <inheritdoc />
        public Cid Id { get; set; }

        /// <inheritdoc />
        public long Size { get; set; }

        /// <summary>
        ///   The name of the node.
        /// </summary>
        /// <value>
        ///   Relative to the containing directory. Defaults to "".
        /// </value>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        ///   The serialised DAG size.
        /// </summary>
        public long DagSize { get; set; }

        /// <inheritdoc />
        public IFileSystemLink ToLink(string name = "")
        {
            return new UnixFsLink
            {
                Name = String.IsNullOrWhiteSpace(name) ? Name : name,
                Id = Id,
                Size = DagSize
            };
        }
    }
}
