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
using System.Threading.Tasks;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Abstractions.Dfs.BlockExchange
{
    /// <summary>
    ///   A content addressable block that is wanted by a peer.
    /// </summary>
    public class WantedBlock
    {
        /// <summary>
        ///   The content ID of the block;
        /// </summary>
        public Cid Id { set; get; }

        /// <summary>
        ///   The peers that want the block.
        /// </summary>
        public List<MultiHash> Peers { set; get; }

        /// <summary>
        ///   The consumers that are waiting for the block.
        /// </summary>
        public List<TaskCompletionSource<IDataBlock>> Consumers { set; get; }
    }
}
