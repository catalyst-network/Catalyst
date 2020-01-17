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
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P.Transports;
using Microsoft.AspNetCore.Mvc;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///     Statistics for bitswap.
    /// </summary>
    public class StatsBitswapDto
    {
        /// <summary>
        ///     TODO: Unknown.
        /// </summary>
        public int ProvideBufLen;

        /// <summary>
        ///     The content IDs that are wanted.
        /// </summary>
        public IEnumerable<BitswapLinkDto> Wantlist;

        /// <summary>
        ///     The known peers.
        /// </summary>
        public IEnumerable<string> Peers;

        /// <summary>
        ///     The number of blocks sent by other peers.
        /// </summary>
        public ulong BlocksReceived;

        /// <summary>
        ///     The number of bytes sent by other peers.
        /// </summary>
        public ulong DataReceived;

        /// <summary>
        ///     The number of blocks sent to other peers.
        /// </summary>
        public ulong BlocksSent;

        /// <summary>
        ///     The number of bytes sent to other peers.
        /// </summary>
        public ulong DataSent;

        /// <summary>
        ///     The number of duplicate blocks sent by other peers.
        /// </summary>
        /// <remarks>
        ///     A duplicate block is a block that is already stored in the
        ///     local repository.
        /// </remarks>
        public ulong DupBlksReceived;

        /// <summary>
        ///     The number of duplicate bytes sent by other peers.
        /// </summary>
        /// <remarks>
        ///     A duplicate block is a block that is already stored in the
        ///     local repository.
        /// </remarks>
        public ulong DupDataReceived;
    }

    /// <summary>
    ///     Get the statistics on various IPFS components.
    /// </summary>
    public class StatsController : IpfsController
    {
        /// <summary>
        ///     Creates a new controller.
        /// </summary>
        public StatsController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///     Get bandwidth information.
        /// </summary>
        [HttpGet] [HttpPost] [Route("stats/bw")]
        public Task<BandwidthData> Bandwidth()
        {
            Response.Headers.Add("X-Chunked-Output", "1");
            return IpfsCore.StatsApi.GetBandwidthStatsAsync(Cancel);
        }

        /// <summary>
        ///     Get bitswap information.
        /// </summary>
        [HttpGet] [HttpPost] [Route("stats/bitswap")] [Route("bitswap/stat")]
        public StatsBitswapDto Bitswap()
        {
            Response.Headers.Add("X-Chunked-Output", "1");
            var data = IpfsCore.StatsApi.GetBitSwapStats(Cancel);
            return new StatsBitswapDto
            {
                BlocksReceived = data.BlocksReceived,
                BlocksSent = data.BlocksSent,
                DataReceived = data.DataReceived,
                DataSent = data.DataSent,
                DupBlksReceived = data.DupBlksReceived,
                DupDataReceived = data.DupDataReceived,
                ProvideBufLen = data.ProvideBufLen,
                Peers = data.Peers.Select(peer => peer.ToString()),
                Wantlist = data.Wantlist.Select(cid => new BitswapLinkDto {Link = cid})
            };
        }

        /// <summary>
        ///     Get repository information.
        /// </summary>
        [HttpGet] [HttpPost] [Route("stats/repo")]
        public Task<RepositoryData> Repo()
        {
            Response.Headers.Add("X-Chunked-Output", "1");
            return IpfsCore.StatsApi.GetRepositoryStatsAsync(Cancel);
        }
    }
}
