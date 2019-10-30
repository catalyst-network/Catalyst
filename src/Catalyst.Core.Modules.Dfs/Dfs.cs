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
using LibP2P;
using Serilog;
using TheDotNetLeague.Ipfs.Core.Lib;
using TheDotNetLeague.Ipfs.Core.Lib.CoreApi;

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
        public async Task<Cid> AddTextAsync(string content, CancellationToken cancellationToken = default)
        {
            var node = await _ipfs.FileSystem.AddTextAsync(
                content,
                AddFileOptions(),
                cancellationToken);
            _logger.Debug("Text added to Dfs with id {0}", node.Id);
            return node.Id;
        }

        /// <inheritdoc />
        public async Task<string> ReadTextAsync(Cid cid,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("Reading content at path {0} from Dfs", cid);
            return await _ipfs.FileSystem.ReadAllTextAsync(cid, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Cid> AddAsync(Stream content,
            string name = "",
            CancellationToken cancellationToken = default)
        {
            var node = await _ipfs.FileSystem
               .AddAsync(content, name, AddFileOptions(), cancellationToken).ConfigureAwait(false);
            _logger.Debug("Content {1} added to Dfs with id {0}",
                node.Id, name + " ");
            return node.Id;
        }

        /// <inheritdoc />
        public async Task<Stream> ReadAsync(Cid cid,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("Reading content at path {0} from Dfs", cid);
            return await _ipfs.FileSystem.ReadFileAsync(cid, cancellationToken).ConfigureAwait(false);
        }
    }
}
