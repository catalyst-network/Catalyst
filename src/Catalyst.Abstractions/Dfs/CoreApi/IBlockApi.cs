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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Manages IPFS blocks.
    /// </summary>
    /// <remarks>
    ///   An IPFS Block is a byte sequence that represents an IPFS Object 
    ///   (i.e. serialized byte buffers). It is useful to talk about them as 
    ///   "blocks" in <see cref="IBitSwapApi">Bitswap</see>
    ///   and other things that do not care about what is being stored. 
    /// </remarks>
    /// <seealso cref="IBlockRepositoryApi"/>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/BLOCK.md">Block API spec</seealso>
    public interface IBlockApi
    {
        IPinApi PinApi { get; set; }
        
        /// <summary>
        ///   Gets an IPFS block.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the block.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous get operation. The task's value
        ///    contains the block's id and data.
        /// </returns>
        Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default);

        /// <summary>
        ///   Stores a byte array as an IPFS block.
        /// </summary>
        /// <param name="data">
        ///   The byte array to send to the IPFS network.
        /// </param>
        /// <param name="contentType">
        ///   The content type or format of the <paramref name="data"/>; such as "raw" or "dag-db".
        ///   See <see cref="MultiCodec"/> for more details.
        /// </param>
        /// <param name="multiHash">
        ///   The <see cref="MultiHash"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="encoding">
        ///   The <see cref="MultiBase"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="pin">
        ///   If <b>true</b> the block is pinned to local storage and will not be
        ///   garbage collected.  The default is <b>false</b>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous put operation. The task's value is
        ///    the block's <see cref="Cid"/>.
        /// </returns>
        Task<Cid> PutAsync(byte[] data,
            string contentType = Cid.DefaultContentType,
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = false,
            CancellationToken cancel = default);

        /// <summary>
        ///   Stores a stream as an IPFS block.
        /// </summary>
        /// <param name="data">
        ///   The <see cref="Stream"/> of data to send to the IPFS network.
        /// </param>
        /// <param name="contentType">
        ///   The content type or format of the <paramref name="data"/>; such as "raw" or "dag-db".
        ///   See <see cref="MultiCodec"/> for more details.
        /// </param>
        /// <param name="multiHash">
        ///   The <see cref="MultiHash"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="encoding">
        ///   The <see cref="MultiBase"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="pin">
        ///   If <b>true</b> the block is pinned to local storage and will not be
        ///   garbage collected.  The default is <b>false</b>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous put operation. The task's value is
        ///    the block's <see cref="Cid"/>.
        /// </returns>
        Task<Cid> PutAsync(Stream data,
            string contentType = Cid.DefaultContentType,
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = false,
            CancellationToken cancel = default);

        /// <summary>
        ///   Information on an IPFS block.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the block.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous operation. The task's value
        ///    contains the block's id and size or <b>null</b>.
        /// </returns>
        /// <remarks>
        ///   Only the local repository is consulted for the block.  If <paramref name="id"/>
        ///   does not exist, then <b>null</b> is retuned.
        /// </remarks>
        Task<IDataBlock> StatAsync(Cid id, CancellationToken cancel = default);

        /// <summary>
        ///   Remove an IPFS block.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the block.
        /// </param>
        /// <param name="ignoreNonexistent">
        ///   If <b>true</b> do not raise exception when <paramref name="id"/> does not
        ///   exist.  Default value is <b>false</b>.
        /// </param>
        /// <returns>
        ///   The awaited Task will return the deleted <paramref name="id"/> or <b>null</b>
        ///   if the <paramref name="id"/> does not exist and <paramref name="ignoreNonexistent"/>
        ///   is <b>true</b>.
        /// </returns>
        /// <remarks>
        ///   This removes the block from the local cache and does not affect other peers.
        /// </remarks>
        Task<Cid> RemoveAsync(Cid id,
            bool ignoreNonexistent = false,
            CancellationToken cancel = default);
    }
}
