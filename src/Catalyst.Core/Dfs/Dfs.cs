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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Config;
using Ipfs;
using Ipfs.CoreApi;
using Serilog;

namespace Catalyst.Core.Dfs
{
    public sealed class Dfs : IDfs
    {
        private static readonly AddFileOptions AddFileOptions = new AddFileOptions
        {
            Hash = MultiHash.GetHashAlgorithmName(Constants.HashAlgorithmType.GetHashCode()),
            RawLeaves = true
        };

        private readonly ICoreApi _ipfs;
        private readonly ILogger _logger;

        public Dfs(ICoreApi ipfsAdapter, ILogger logger)
        {
            _ipfs = ipfsAdapter;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<string> AddTextAsync(string content, CancellationToken cancellationToken = default)
        {
            var node = await _ipfs.FileSystem.AddTextAsync(
                content,
                options: AddFileOptions,
                cancel: cancellationToken);
            var id = node.Id.Encode();
            _logger.Debug("Text added to IPFS with id {0}", id);
            return id;
        }

        /// <inheritdoc />
        public Task<string> ReadTextAsync(string id,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("Reading content at path {0} from IPFS", id);
            return _ipfs.FileSystem.ReadAllTextAsync(id, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<string> AddAsync(Stream content,
            string name = "",
            CancellationToken cancellationToken = default)
        {
            var node = await _ipfs.FileSystem
               .AddAsync(content, name, AddFileOptions, cancellationToken);
            var id = node.Id.Encode();
            _logger.Debug("Content {1}added to IPFS with id {0}",
                id, name + " ");
            return id;
        }

        /// <inheritdoc />
        public Task<Stream> ReadAsync(string id,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("Reading content at path {0} from Ipfs", id);
            return _ipfs.FileSystem.ReadFileAsync(id, cancellationToken);
        }
    }
}
