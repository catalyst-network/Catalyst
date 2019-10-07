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
using Catalyst.Abstractions.Hashing;
using Ipfs;
using Ipfs.CoreApi;
using Serilog;

namespace Catalyst.Core.Modules.Dfs
{
    public sealed class Dfs : IDfs
    {
        private readonly ICoreApi _ipfs;
        private readonly IHashProvider _hashProvider;
        private readonly ILogger _logger;

        public Dfs(ICoreApi ipfsAdapter, IHashProvider hashProvider, ILogger logger)
        {
            _ipfs = ipfsAdapter;
            _hashProvider = hashProvider;
            _logger = logger;
        }

        private AddFileOptions AddFileOptions()
        {
            return new AddFileOptions
            {
                Hash = _hashProvider.HashingAlgorithm.Name,
                RawLeaves = true
            };
        }

        /// <inheritdoc />
        public async Task<MultiHash> AddTextAsync(string content, CancellationToken cancellationToken = default)
        {
            var node = await _ipfs.FileSystem.AddTextAsync(
                content,
                AddFileOptions(),
                cancellationToken);
            var id = node.Id.Encode();
            _logger.Debug("Text added to IPFS with id {0}", id);
            return id;
        }

        /// <inheritdoc />
        public Task<string> ReadTextAsync(MultiHash hash,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("Reading content at path {0} from IPFS", hash.ToBase32());
            return _ipfs.FileSystem.ReadAllTextAsync(hash.ToBase32(), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<MultiHash> AddAsync(Stream content,
            string name = "",
            CancellationToken cancellationToken = default)
        {
            var node = await _ipfs.FileSystem
               .AddAsync(content, name, AddFileOptions(), cancellationToken);
            var id = node.Id.Encode();
            _logger.Debug("Content {1}added to IPFS with id {0}",
                id, name + " ");
            return id;
        }

        /// <inheritdoc />
        public Task<Stream> ReadAsync(MultiHash hash,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("Reading content at path {0} from Ipfs", hash.ToBase32());
            return _ipfs.FileSystem.ReadFileAsync(hash.ToBase32(), cancellationToken);
        }
    }
}
