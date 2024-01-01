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

using System.IO;

namespace Lib.P2P
{
    /// <summary>
    ///   represents some data dfs
    /// </summary>
    /// <remarks>
    ///   A <b>DataBlock</b> has an <see cref="Id">unique ID</see>
    ///   and some data (<see cref="IDataBlock.DataBytes"/> 
    ///   or <see cref="IDataBlock.DataStream"/>).
    ///   <para>
    ///   It is useful to talk about them as "blocks" in Bitswap 
    ///   and other things that do not care about what is being stored.
    ///   </para>
    /// </remarks>
    /// <seealso>
    ///     <cref>Catalyst.Ipfs.Core.IMerkleNode{Link}</cref>
    /// </seealso>
    public interface IDataBlock
    {
        /// <summary>
        ///   Contents as a byte array.
        /// </summary>
        /// <remarks>
        ///   It is never <b>null</b>.
        /// </remarks>
        /// <value>
        ///   The contents as a sequence of bytes.
        /// </value>
        byte[] DataBytes { get; }

        /// <summary>
        ///   Contents as a stream of bytes.
        /// </summary>
        /// <value>
        ///   The contents as a stream of bytes.
        /// </value>
        Stream DataStream { get; }

        /// <summary>
        ///   The unique ID of the data.
        /// </summary>
        /// <value>
        ///   A <see cref="Lib.P2P.Cid"/> of the content.
        /// </value>
        Cid Id { get; }

        /// <summary>
        ///   The size (in bytes) of the data.
        /// </summary>
        /// <value>Number of bytes.</value>
        long Size { get; }
    }
}
