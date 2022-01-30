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

// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Catalyst.Abstractions.Dfs.CoreApi;
// using Microsoft.AspNetCore.Mvc;
// using MultiFormats;
//
// namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
// {
//     /// <summary>
//     ///   Some miscellaneous methods.
//     /// </summary>
//     public class GenericController : IpfsController
//     {
//         /// <summary>
//         ///   Creates a new instance of the controller.
//         /// </summary>
//         public GenericController(ICoreApi ipfs) : base(ipfs) { }
//
//         /// <summary>
//         ///   Information about the peer.
//         /// </summary>
//         /// <param name="arg">
//         ///   The peer's ID or empty for the local peer.
//         /// </param>
//         [HttpGet, HttpPost, Route("id")]
//         public async Task<PeerInfoDto> Get(string arg)
//         {
//             MultiHash id = null;
//             if (!String.IsNullOrEmpty(arg))
//                 id = arg;
//
//             var peer = await IpfsCore.Generic.IdAsync(id, Cancel);
//             return new PeerInfoDto(peer);
//         }
//
//         /// <summary>
//         ///   Version information on the local peer.
//         /// </summary>
//         [HttpGet, HttpPost, Route("version")]
//         public async Task<Dictionary<string, string>> Version() { return await IpfsCore.Generic.VersionAsync(Cancel); }
//
//         /// <summary>
//         ///   Resolve a name.
//         /// </summary>
//         /// <param name="arg">
//         ///   The name to resolve. Can be CID + [/path], "/ipfs/..." or
//         ///   "/ipns/...".
//         /// </param>
//         /// <param name="recursive">
//         ///   Resolve until the result is an IPFS name. Defaults to <b>false</b>.
//         /// </param>
//         [HttpGet(), HttpPost(), Route("resolve")]
//         public async Task<PathDto> Resolve(string arg, bool recursive = false)
//         {
//             var path = await IpfsCore.Generic.ResolveAsync(arg, recursive, Cancel);
//             return new PathDto(path);
//         }
//
//         /// <summary>
//         ///  Stop the IPFS peer.
//         /// </summary>
//         /// <returns></returns>
//         [HttpGet, HttpPost, Route("shutdown")]
//         public async Task Shutdown()
//         {
//             await IpfsCore.Generic.ShutdownAsync();
//
//             Program.Shutdown();
//         }
//     }
// }

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers {}
