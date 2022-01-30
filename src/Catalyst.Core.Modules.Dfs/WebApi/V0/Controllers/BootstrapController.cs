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
using Microsoft.AspNetCore.Mvc;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///   Manages the list of initial peers.
    /// </summary>
    /// <remarks>
    ///  The API manipulates the "bootstrap list", which contains
    ///  the addresses of the bootstrap nodes. These are the trusted peers from
    ///  which to learn about other peers in the network.
    /// </remarks>
    public sealed class BootstrapController : DfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public BootstrapController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///   List all the bootstrap peers.
        /// </summary>
        [HttpGet, HttpPost, Route("bootstrap/list")]
        public async Task<BootstrapPeersDto> List()
        {
            var peers = await DfsService.BootstrapApi.ListAsync(Cancel);
            return new BootstrapPeersDto
            {
                Peers = peers.Select(peer => peer.ToString())
            };
        }

        /// <summary>
        ///   Remove all the bootstrap peers.
        /// </summary>
        [HttpGet, HttpPost, Route("bootstrap/rm/all")]
        public async Task RemoveAll() { await DfsService.BootstrapApi.RemoveAllAsync(Cancel); }

        /// <summary>
        ///   Add the default bootstrap peers.
        /// </summary>
        [HttpGet, HttpPost, Route("bootstrap/add/default")]
        public async Task<BootstrapPeersDto> AddDefaults()
        {
            var peers = await DfsService.BootstrapApi.AddDefaultsAsync(Cancel);
            return new BootstrapPeersDto
            {
                Peers = peers.Select(peer => peer.ToString())
            };
        }

        /// <summary>
        ///   Add a bootstrap peer.
        /// </summary>
        /// <param name="arg">
        ///   The multiaddress of the peer.
        /// </param>
        /// <param name="default">
        ///   If <b>true</b>, add all the default bootstrap peers.
        /// </param>
        [HttpGet, HttpPost, Route("bootstrap/add")]
        public async Task<BootstrapPeersDto> Add(string arg,
            bool @default = false)
        {
            if (@default)
            {
                var peers = await DfsService.BootstrapApi.AddDefaultsAsync(Cancel);
                return new BootstrapPeersDto
                {
                    Peers = peers.Select(p => p.ToString())
                };
            }

            var peer = await DfsService.BootstrapApi.AddAsync(arg, Cancel);
            return new BootstrapPeersDto
            {
                Peers = new[] {peer?.ToString()}
            };
        }

        /// <summary>
        ///   Remove a bootstrap peer.
        /// </summary>
        /// <param name="arg">
        ///   The multiaddress of the peer.
        /// </param>
        /// <param name="all">
        ///   If <b>true</b>, remove all the bootstrap peers.
        /// </param>
        [HttpGet, HttpPost, Route("bootstrap/rm")]
        public async Task<BootstrapPeersDto> Remove(string arg,
            bool all = false)
        {
            if (all)
            {
                await DfsService.BootstrapApi.RemoveAllAsync(Cancel);
                return new BootstrapPeersDto {Peers = new string[0]};
            }

            var peer = await DfsService.BootstrapApi.RemoveAsync(arg, Cancel);
            return new BootstrapPeersDto
            {
                Peers = new[] {peer?.ToString()}
            };
        }
    }
}
