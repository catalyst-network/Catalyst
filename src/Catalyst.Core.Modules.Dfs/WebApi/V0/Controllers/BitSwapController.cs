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
using Catalyst.Core.Modules.Dfs.WebApi.V0.Dto;
using Lib.P2P;
using Microsoft.AspNetCore.Mvc;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///     Data trading module for IPFS. Its purpose is to request blocks from and
    ///     send blocks to other peers in the network.
    /// </summary>
    /// <remarks>
    ///     Bitswap has two primary jobs (1) Attempt to acquire blocks from the network that
    ///     have been requested by the client and (2) Judiciously(though strategically)
    ///     send blocks in its possession to other peers who want them.
    /// </remarks>
    public sealed class BitSwapController : DfsController
    {
        /// <summary>
        ///     Creates a new controller.
        /// </summary>
        public BitSwapController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///     The blocks that are needed by a peer.
        /// </summary>
        /// <param name="arg">
        ///     A peer ID or empty for self.
        /// </param>
        [HttpGet] [HttpPost] [Route("bitswap/wantlist")]
        public async Task<BitSwapWantsDto> Wants(string arg)
        {
            var peer = string.IsNullOrEmpty(arg) ? null : new MultiHash(arg);
            var cids = await DfsService.BitSwapApi.WantsAsync(peer, Cancel);
            return new BitSwapWantsDto
            {
                Keys = cids.Select(cid => new BitSwapLinkDto
                {
                    Link = cid
                }).ToList()
            };
        }

        /// <summary>
        ///     Remove the CID from the want list.
        /// </summary>
        /// <param name="arg">
        ///     The CID that is no longer needed.
        /// </param>
        [HttpGet] [HttpPost] [Route("bitswap/unwant")]
        public void Unwants(string arg) { DfsService.BitSwapApi.UnWant(arg, Cancel); }

        /// <summary>
        ///     The blocks that are needed by a peer.
        /// </summary>
        /// <param name="arg">
        ///     A peer ID.
        /// </param>
        [HttpGet] [HttpPost] [Route("bitswap/ledger")]
        public BitSwapLedgerDto Ledger(string arg)
        {
            var peer = new Peer
            {
                Id = arg
            };
            var ledger = DfsService.BitSwapApi.GetBitSwapLedger(peer, Cancel);
            return new BitSwapLedgerDto
            {
                Peer = ledger.Peer.Id.ToBase58(),
                Exchanged = ledger.BlocksExchanged,
                Recv = ledger.DataReceived,
                Sent = ledger.DataSent,
                Value = ledger.DebtRatio
            };
        }

        // "bitswap/stat" is handled by the StatsController.
    }
}
