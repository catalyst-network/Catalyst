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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Modules.Dfs.WebApi.V0.Dto;
using Microsoft.AspNetCore.Mvc;

// TODO: need MultiAddress.WithOutPeer (should be in DFS code)

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///   Distributed Hash Table.
    /// </summary>
    /// <remarks>
    ///   The DHT is a place to store, not the value, but pointers to peers who have 
    ///   the actual value.
    /// </remarks>
    public sealed class DhtController : DfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public DhtController(ICoreApi dfs) : base(dfs) { }

        /// <summary>
        ///   Query the DHT for all of the multiaddresses associated with a Peer ID.
        /// </summary>
        /// <param name="arg">
        ///   The peer ID to find.
        /// </param>
        /// <returns>
        ///   Information about the peer.
        /// </returns>
        [HttpGet, HttpPost, Route("dht/findpeer")]
        public async Task<DhtPeerDto> FindPeer(string arg)
        {
            var peer = await DfsService.DhtApi.FindPeerAsync(arg, Cancel);
            return new DhtPeerDto
            {
                Id = peer.Id.ToBase58(),
                Responses = new[]
                {
                    new DhtPeerResponseDto
                    {
                        Id = peer.Id.ToBase58(),
                        Addrs = peer.Addresses.Select(a => a.WithoutPeerId().ToString())
                    }
                }
            };
        }

        /// <summary>
        ///  Find peers in the DHT that can provide a specific value, given a key.
        /// </summary>
        /// <param name="arg">
        ///   The CID key,
        /// </param>
        /// <param name="limit">
        ///   The maximum number of providers to find.
        /// </param>
        /// <returns>
        ///   Information about the peer providers.
        /// </returns>
        [HttpGet, HttpPost, Route("dht/findprovs")]
        public async Task<IEnumerable<DhtPeerDto>> FindProviders(string arg,
            [ModelBinder(Name = "num-providers")] int limit = 20)
        {
            var peers = await DfsService.DhtApi.FindProvidersAsync(arg, limit, null, Cancel);
            return peers.Select(peer => new DhtPeerDto
            {
                Id = peer.Id.ToBase58(), // TODO: should be the peer ID that answered the query
                Responses = new[]
                {
                    new DhtPeerResponseDto
                    {
                        Id = peer.Id.ToBase58(),
                        Addrs = peer.Addresses.Select(a => a.WithoutPeerId().ToString())
                    }
                }
            });
        }
    }
}
