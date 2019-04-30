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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Ipfs.CoreApi;
using Serilog;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class IpfsDfs : IDfs
    {
        public static readonly string HashAlgorithm = "blake2b-256";

        public static readonly AddFileOptions AddFileOptions = new AddFileOptions
        {
            Hash = HashAlgorithm,
            RawLeaves = true
        };

        private readonly IIpfsEngine _ipfsEngine;

        private readonly ILogger _logger;

        public IpfsDfs(IIpfsEngine ipfsEngine, ILogger logger)
        {
            _ipfsEngine = ipfsEngine;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<string> AddTextAsync(string content, CancellationToken cancellationToken = default)
        {
            var node = await _ipfsEngine.FileSystem.AddTextAsync(
                content,
                options: AddFileOptions,
                cancel: cancellationToken);
            var id = node.Id.Encode();
            _logger.Debug("Text added to IPFS with id {0}", id);
            return id;
        }

        /// <inheritdoc />
        public async Task<string> ReadTextAsync(string id,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("Reading content at path {0} from IPFS", id);
            return await _ipfsEngine.FileSystem.ReadAllTextAsync(id, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<string> AddAsync(Stream content,
            string name = "",
            CancellationToken cancellationToken = default)
        {
            var node = await _ipfsEngine.FileSystem
               .AddAsync(content, name, AddFileOptions, cancellationToken);
            var id = node.Id.Encode();
            _logger.Debug("Content {1}added to IPFS with id {0}",
                id, name + " ");
            return id;
        }

        /// <inheritdoc />
        public async Task<Stream> ReadAsync(string id,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("Reading content at path {0} from Ipfs", id);
            return await _ipfsEngine.FileSystem.ReadFileAsync(id, cancellationToken);
        }

    }
}
