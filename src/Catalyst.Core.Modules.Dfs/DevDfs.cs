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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Modules.Dfs.Extensions;
using Dawn;
using LibP2P;

namespace Catalyst.Core.Modules.Dfs
{
    /// <summary>
    ///     A very naive implementation of the IDfs interface that simply relies on the file system.
    ///     This can only result in a 'Distributed' file system if the <see cref="_baseFolder" /> happens
    ///     to be shared network path. Otherwise, this can be used in integration tests, to ensure
    ///     the tests can be run locally and offline.
    /// </summary>
    /// <remarks>
    ///     The hashing algorithm is also a simple one (<see cref="Multiformats.Hash.Algorithms.BLAKE2B_32" />>) to save time
    ///     in integration tests
    /// </remarks>
    /// <inheritdoc cref="IDfs" />
    public sealed class DevDfs : IDfs
    {
        private readonly DirectoryInfo _baseFolder;
        private readonly IFileSystem _fileSystem;
        private readonly IHashProvider _hashProvider;

        public DevDfs(IFileSystem fileSystem,
            IHashProvider hashProvider,
            string baseFolder = null)
        {
            Guard.Argument(hashProvider.HashingAlgorithm, nameof(hashProvider.HashingAlgorithm))
               .Require(h => h.DigestSize <= 159, h =>
                    "The hashing algorithm needs to produce file names smaller than 255 base 32 characters or 160 bytes" +
                    $"but the default length for {hashProvider.HashingAlgorithm.GetType().Name} is {h.DigestSize}.");

            var dfsBaseFolder = baseFolder ?? Path.Combine(fileSystem.GetCatalystDataDir().FullName,
                Constants.DfsDataSubDir);

            _baseFolder = new DirectoryInfo(dfsBaseFolder);
            if (!_baseFolder.Exists)
            {
                _baseFolder.Create();
            }

            _fileSystem = fileSystem;
            _hashProvider = hashProvider;
        }

        /// <inheritdoc />
        public async Task<Cid> AddTextAsync(string utf8Content, CancellationToken cancellationToken = default)
        {
            var cid = _hashProvider.ComputeUtf8MultiHash(utf8Content).CreateCid();
            var filePath = Path.Combine(_baseFolder.FullName, cid);

            await _fileSystem.File.WriteAllTextAsync(
                filePath,
                utf8Content, Encoding.UTF8, cancellationToken);

            return cid;
        }

        /// <inheritdoc />
        public async Task<string> ReadTextAsync(Cid cid, CancellationToken cancellationToken = default)
        {
            return await _fileSystem.File.ReadAllTextAsync(Path.Combine(_baseFolder.FullName, cid),
                Encoding.UTF8,
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Cid> AddAsync(Stream content,
            string name = "",
            CancellationToken cancellationToken = default)
        {
            var cid = _hashProvider.ComputeMultiHash(content).CreateCid();
            var filePath = Path.Combine(_baseFolder.FullName, cid);

            using (var file = _fileSystem.File.Create(filePath))
            {
                content.Position = 0;
                await content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            }

            return cid;
        }

        /// <inheritdoc />
        public async Task<Stream> ReadAsync(Cid cid, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_fileSystem.File.OpenRead(Path.Combine(_baseFolder.FullName, cid)))
               .ConfigureAwait(false);
        }
    }
}
