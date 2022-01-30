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

using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Modules.Dfs.WebApi.V0.Dto;
using Lib.P2P.Transports;
using Microsoft.AspNetCore.Mvc;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///     Get the statistics on various IPFS components.
    /// </summary>
    public class StatsController : DfsController
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
            return DfsService.StatsApi.GetBandwidthStatsAsync(Cancel);
        }

        /// <summary>
        ///     Get bitswap information.
        /// </summary>
        [HttpGet] [HttpPost] [Route("stats/bitswap")] [Route("bitswap/stat")]
        public StatsBitSwapDto Bitswap()
        {
            Response.Headers.Add("X-Chunked-Output", "1");
            var data = DfsService.StatsApi.GetBitSwapStats(Cancel);
            return new StatsBitSwapDto
            {
                BlocksReceived = data.BlocksReceived,
                BlocksSent = data.BlocksSent,
                DataReceived = data.DataReceived,
                DataSent = data.DataSent,
                DupBlksReceived = data.DupBlksReceived,
                DupDataReceived = data.DupDataReceived,
                ProvideBufLen = data.ProvideBufLen,
                Peers = data.Peers.Select(peer => peer.ToString()),
                Wantlist = data.Wantlist.Select(cid => new BitSwapLinkDto {Link = cid})
            };
        }

        /// <summary>
        ///     Get repository information.
        /// </summary>
        [HttpGet] [HttpPost] [Route("stats/repo")]
        public Task<RepositoryData> Repo()
        {
            Response.Headers.Add("X-Chunked-Output", "1");
            return DfsService.StatsApi.GetRepositoryStatsAsync(Cancel);
        }
    }
}
