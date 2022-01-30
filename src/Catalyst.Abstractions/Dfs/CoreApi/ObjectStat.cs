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

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Information on a DAG node.
    /// </summary>
    /// <seealso cref="IObjectApi"/>
    public class ObjectStat
    {
        /// <summary>
        ///   Number of links.
        /// </summary>
        public int LinkCount { get; set; }

        /// <summary>
        ///   Size of the links segment.
        /// </summary>
        public long LinkSize { get; set; }

        /// <summary>
        ///   Size of the raw, encoded data.
        /// </summary>
        public long BlockSize { get; set; }

        /// <summary>
        ///   Siz of the data segment.
        /// </summary>
        public long DataSize { get; set; }

        /// <summary>
        ///   Size of object and its references
        /// </summary>
        public long CumulativeSize { get; set; }
    }
}
