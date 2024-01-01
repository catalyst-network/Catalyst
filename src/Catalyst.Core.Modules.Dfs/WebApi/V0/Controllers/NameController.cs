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

using System;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Dfs.WebApi.V0.Dto;
using Lib.P2P;
using Microsoft.AspNetCore.Mvc;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///     Manages the IPNS (Interplanetary Name Space).
    /// </summary>
    /// <remarks>
    ///     IPNS is a PKI namespace, where names are the hashes of public keys, and
    ///     the private key enables publishing new(signed) values. The default name
    ///     is the node's own <see cref="Peer.Id" />,
    ///     which is the hash of its public key.
    /// </remarks>
    public sealed class NameController : DfsController
    {
        /// <summary>
        ///     Creates a new controller.
        /// </summary>
        public NameController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///     Resolve a name.
        /// </summary>
        [HttpGet] [HttpPost] [Route("name/resolve")]
        public async Task<PathDto> Resolve(string arg,
            bool recursive = false,
            bool nocache = false)
        {
            var path = await DfsService.NameApi.ResolveAsync(arg, recursive, nocache, Cancel);
            return new PathDto(path);
        }

        /// <summary>
        ///     Publish content.
        /// </summary>
        /// <param name="arg">
        ///     The CID or path to the content to publish.
        /// </param>
        /// <param name="resolve">
        ///     Resolve before publishing.
        /// </param>
        /// <param name="key">
        ///     The local key name used to sign the content.
        /// </param>
        /// <param name="lifetime">
        ///     Duration that the record will be valid for.
        /// </param>
        [HttpGet] [HttpPost] [Route("name/publish")]
        public async Task<NamedContentDto> Publish(string arg,
            bool resolve = true,
            string key = "self",
            string lifetime = "24h")
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                throw new ArgumentNullException(nameof(arg), "The name is required.");
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(arg), "The key name is required.");
            }

            if (string.IsNullOrWhiteSpace(lifetime))
            {
                throw new ArgumentNullException(nameof(arg), "The lifetime is required.");
            }

            var duration = Duration.Parse(lifetime);
            var content = await DfsService.NameApi.PublishAsync(arg, resolve, key, duration, Cancel);
            return new NamedContentDto
            {
                Name = content.NamePath,
                Value = content.ContentPath
            };
        }
    }
}
