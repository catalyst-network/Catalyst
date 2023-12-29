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
using MultiFormats;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   The statistics for <see cref="IStatsApi.GetBitSwapStats"/>.
    /// </summary>
    public class BitswapData
    {
        /// <summary>
        ///   TODO: Unknown.
        /// </summary>
        public int ProvideBufLen;

        /// <summary>
        ///   The content that is wanted.
        /// </summary>
        public IEnumerable<Cid> Wantlist;

        /// <summary>
        ///   The known peers.
        /// </summary>
        public IEnumerable<MultiHash> Peers;

        /// <summary>
        ///   The number of blocks sent by other peers.
        /// </summary>
        public ulong BlocksReceived;

        /// <summary>
        ///   The number of bytes sent by other peers.
        /// </summary>
        public ulong DataReceived;

        /// <summary>
        ///   The number of blocks sent to other peers.
        /// </summary>
        public ulong BlocksSent;

        /// <summary>
        ///   The number of bytes sent to other peers.
        /// </summary>
        public ulong DataSent;

        /// <summary>
        ///   The number of duplicate blocks sent by other peers.
        /// </summary>
        /// <remarks>
        ///   A duplicate block is a block that is already stored in the
        ///   local repository.
        /// </remarks>
        public ulong DupBlksReceived;

        /// <summary>
        ///   The number of duplicate bytes sent by other peers.
        /// </summary>
        /// <remarks>
        ///   A duplicate block is a block that is already stored in the
        ///   local repository.
        /// </remarks>
        public ulong DupDataReceived;
    }
}
