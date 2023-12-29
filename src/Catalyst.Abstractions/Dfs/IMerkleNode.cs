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

using System.Collections.Generic;
using Lib.P2P;

namespace Catalyst.Abstractions.Dfs
{
    /// <summary>
    ///   A Directed Acyclic Graph (DAG) in IPFS.
    /// </summary>
    /// <remarks>
    ///   A <b>MerkleNode</b> has a sequence of navigable <see cref="Links"/>
    ///   and some data (<see cref="IDataBlock.DataBytes"/> 
    ///   or <see cref="IDataBlock.DataStream"/>).
    /// </remarks>
    /// <typeparam name="TLink">
    ///   The type of <see cref="IMerkleLink"/> used by this node.
    /// </typeparam>
    /// <seealso href="https://en.wikipedia.org/wiki/Directed_acyclic_graph"/>
    /// <seealso href="https://github.com/ipfs/specs/tree/master/merkledag"/>
    public interface IMerkleNode<out TLink> : IDataBlock
        where TLink : IMerkleLink
    {
        /// <summary>
        ///   Links to other nodes.
        /// </summary>
        /// <value>
        ///   A sequence of <typeparamref name="TLink"/>.
        /// </value>
        /// <remarks>
        ///   It is never <b>null</b>.
        ///   <para>
        ///   The links are sorted ascending by <see cref="IMerkleLink.Name"/>. A <b>null</b>
        ///   name is compared as "".
        ///   </para>
        /// </remarks>
        IEnumerable<TLink> Links { get; }

        /// <summary>
        ///   Returns a link to the node.
        /// </summary>
        /// <param name="name">
        ///   A <see cref="IMerkleLink.Name"/> for the link; defaults to "".
        /// </param>
        /// <returns>
        ///   A new <see cref="IMerkleLink"/> to the node.
        /// </returns>
        TLink ToLink(string name = "");
    }
}
