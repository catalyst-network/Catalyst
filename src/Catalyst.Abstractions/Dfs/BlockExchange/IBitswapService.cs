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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.BlockExchange.Protocols;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Abstractions.Dfs.BlockExchange
{
    public interface IBitswapService : IService
    {
        /// <summary>
        ///   The supported bitswap protocols.
        /// </summary>
        /// <value>
        ///   Defaults to <see cref="Bitswap11"/> and <see cref="Bitswap1"/>.
        /// </value>
        IBitswapProtocol[] Protocols { get; set; }

        /// <summary>
        ///   Provides access to other peers.
        /// </summary>
        SwarmService SwarmService { get; set; }

        /// <summary>
        ///   Provides access to blocks of data.
        /// </summary>
        IBlockApi BlockService { get; set; }

        /// <summary>
        ///   Statistics on the bitswap component.
        /// </summary>
        /// <seealso cref="IStatsApi"/>
        BitswapData Statistics { get; }

        /// <summary>
        ///   Gets the bitswap ledger for the specified peer.
        /// </summary>
        /// <param name="peer">
        ///   The peer to get information on.  If the peer is unknown, then a ledger
        ///   with zeros is returned.
        /// </param>
        /// <returns>
        ///   Statistics on the bitswap blocks exchanged with the peer.
        /// </returns>
        /// <seealso cref="IBitSwapApi.GetBitSwapLedger"/>
        BitswapLedger PeerLedger(Peer peer);

        /// <summary>
        ///   Raised when a blocked is needed.
        /// </summary>
        /// <remarks>
        ///   Only raised when a block is first requested.
        /// </remarks>
        event EventHandler<CidEventArgs> BlockNeeded;

        /// <summary>
        ///   The blocks needed by the peer.
        /// </summary>
        /// <param name="peer">
        ///   The unique ID of the peer.
        /// </param>
        /// <returns>
        ///   The sequence of CIDs need by the <paramref name="peer"/>.
        /// </returns>
        IEnumerable<Cid> PeerWants(MultiHash peer);

        /// <summary>
        ///   Adds a block to the want list.
        /// </summary>
        /// <param name="id">
        ///   The CID of the block to add to the want list.
        /// </param>
        /// <param name="peer">
        ///   The unique ID of the peer that wants the block.  This is for
        ///   information purposes only.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the contents of block.
        /// </returns>
        /// <remarks>
        ///   Other peers are informed that the block is needed by this peer. Hopefully,
        ///   someone will forward it to us.
        ///   <para>
        ///   Besides using <paramref name="cancel"/> for cancellation, the 
        ///   <see cref="Catalyst.Core.Modules.Dfs.BlockExchange.Bitswap.Unwant(Lib.P2P.Cid)"/> method will also cancel the operation.
        ///   </para>
        /// </remarks>
        Task<IDataBlock> WantAsync(Cid id, MultiHash peer, CancellationToken cancel);

        /// <summary>
        ///   Removes the block from the want list.
        /// </summary>
        /// <param name="id">
        ///   The CID of the block to remove from the want list.
        /// </param>
        /// <remarks>
        ///   Any tasks waiting for the block are cancelled.
        ///   <para>
        ///   No exception is thrown if the <paramref name="id"/> is not
        ///   on the want list.
        ///   </para>
        /// </remarks>
        void Unwant(Cid id);
        
        /// <summary>
        ///   Indicate that a remote peer sent a block.
        /// </summary>
        /// <param name="remote">
        ///   The peer that sent the block.
        /// </param>
        /// <param name="block">
        ///   The data for the block.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///   Updates the statistics.
        ///   </para>
        ///   <para>
        ///   If the block is acceptable then the <paramref name="block"/> is added to local cache
        ///   via the <see cref="Catalyst.Core.Modules.Dfs.BlockExchange.Bitswap.BlockService"/>.
        ///   </para>
        /// </remarks>
        Task OnBlockReceivedAsync(Peer remote, byte[] block);

        /// <summary>
        ///   Indicate that a remote peer sent a block.
        /// </summary>
        /// <param name="remote">
        ///   The peer that sent the block.
        /// </param>
        /// <param name="block">
        ///   The data for the block.
        /// </param>
        /// <param name="contentType">
        ///   The <see cref="Cid.ContentType"/> of the block.
        /// </param>
        /// <param name="multiHash">
        ///   The multihash algorithm name of the block.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///   Updates the statistics.
        ///   </para>
        ///   <para>
        ///   If the block is acceptable then the <paramref name="block"/> is added to local cache
        ///   via the <see cref="Catalyst.Core.Modules.Dfs.BlockExchange.Bitswap.BlockService"/>.
        ///   </para>
        /// </remarks>
        Task OnBlockReceivedAsync(Peer remote, byte[] block, string contentType, string multiHash);

        /// <summary>
        ///   Indicate that the local peer sent a block to a remote peer.
        /// </summary>
        /// <param name="remote">
        ///   The peer that sent the block.
        /// </param>
        /// <param name="block">
        ///   The data for the block.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task OnBlockSentAsync(Peer remote, IDataBlock block);

        /// <summary>
        ///   Indicate that a block is found.
        /// </summary>
        /// <param name="block">
        ///   The block that was found.
        /// </param>
        /// <returns>
        ///   The number of consumers waiting for the <paramref name="block"/>.
        /// </returns>
        /// <remarks>
        ///   <b>Found</b> should be called whenever a new block is discovered. 
        ///   It will continue any Task that is waiting for the block and
        ///   remove the block from the want list.
        /// </remarks>
        int Found(IDataBlock block);
    }
}
