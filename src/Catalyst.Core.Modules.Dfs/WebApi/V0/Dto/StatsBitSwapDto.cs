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

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Dto
{
    /// <summary>
    ///     Statistics for bitswap.
    /// </summary>
    public class StatsBitSwapDto
    {
        /// <summary>
        ///     TODO: Unknown.
        /// </summary>
        public int ProvideBufLen { set; get; }

        /// <summary>
        ///     The content IDs that are wanted.
        /// </summary>
        public IEnumerable<BitSwapLinkDto> Wantlist { set; get; }

        /// <summary>
        ///     The known peers.
        /// </summary>
        public IEnumerable<string> Peers { set; get; }

        /// <summary>
        ///     The number of blocks sent by other peers.
        /// </summary>
        public ulong BlocksReceived { set; get; }

        /// <summary>
        ///     The number of bytes sent by other peers.
        /// </summary>
        public ulong DataReceived { set; get; }

        /// <summary>
        ///     The number of blocks sent to other peers.
        /// </summary>
        public ulong BlocksSent { set; get; }

        /// <summary>
        ///     The number of bytes sent to other peers.
        /// </summary>
        public ulong DataSent { set; get; }

        /// <summary>
        ///     The number of duplicate blocks sent by other peers.
        /// </summary>
        /// <remarks>
        ///     A duplicate block is a block that is already stored in the
        ///     local repository.
        /// </remarks>
        public ulong DupBlksReceived { set; get; }

        /// <summary>
        ///     The number of duplicate bytes sent by other peers.
        /// </summary>
        /// <remarks>
        ///     A duplicate block is a block that is already stored in the
        ///     local repository.
        /// </remarks>
        public ulong DupDataReceived { set; get; }
    }
}
