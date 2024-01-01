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

using Lib.P2P;

namespace Catalyst.Abstractions.Dfs
{
    /// <summary>
    ///   A link to another node in IPFS.
    /// </summary>
    public interface IMerkleLink
    {
        /// <summary>
        ///   A name associated with the linked node.
        /// </summary>
        /// <value>A <see cref="string"/> or <b>null</b>.</value>
        /// <remarks>
        ///   <note type="warning">
        ///   IPFS considers a <b>null</b> name different from a <see cref="string.Empty"/>
        ///   name;
        ///   </note>
        /// </remarks>
        string Name { get; }

        /// <summary>
        ///   The unique ID of the link.
        /// </summary>
        /// <value>
        ///   A <see cref="Cid"/> of the content.
        /// </value>
        Cid Id { get; }

        /// <summary>
        ///   The serialised size (in bytes) of the linked node.
        /// </summary>
        /// <value>Number of bytes.</value>
        long Size { get; }
    }
}
