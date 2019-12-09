﻿using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Dfs.Migration;
using Catalyst.Abstractions.Options;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class BlockRepositoryApi : IBlockRepositoryApi
    {
        private IPinApi _pinApi;
        private IBlockApi _blockApi;
        private readonly IMigrationManager _migrationManager;
        private readonly RepositoryOptions _repositoryOptions;

        public BlockRepositoryApi(IPinApi pinApi, IBlockApi blockApi, IMigrationManager migrationManager, RepositoryOptions repositoryOptions)
        {
            _pinApi = pinApi;
            _blockApi = blockApi;
            _migrationManager = migrationManager;
            _repositoryOptions = repositoryOptions;
        }

        public async Task RemoveGarbageAsync(CancellationToken cancel = default(CancellationToken))
        {
            var blockApi = (BlockApi) _blockApi;
            var pinApi = (PinApi) _pinApi;
            foreach (var cid in blockApi.Store.Names)
            {
                if (!await pinApi.IsPinnedAsync(cid, cancel).ConfigureAwait(false))
                {
                    await _blockApi.RemoveAsync(cid, ignoreNonexistent: true, cancel: cancel).ConfigureAwait(false);
                }
            }
        }

        public async Task<RepositoryData> StatisticsAsync(CancellationToken cancel = default(CancellationToken))
        {
            var data = new RepositoryData
            {
                RepoPath = Path.GetFullPath(_repositoryOptions.Folder),
                Version = await VersionAsync(cancel).ConfigureAwait(false),
                StorageMax = 10000000000 // TODO: there is no storage max
            };

            var blockApi = (BlockApi) _blockApi;
            GetDirStats(blockApi.Store.Folder, data, cancel);

            return data;
        }

        public Task VerifyAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<string> VersionAsync(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(_migrationManager.CurrentVersion.ToString(CultureInfo.InvariantCulture));
        }

        void GetDirStats(string path, RepositoryData data, CancellationToken cancel)
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                cancel.ThrowIfCancellationRequested();
                ++data.NumObjects;
                data.RepoSize += (ulong) (new FileInfo(file).Length);
            }

            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                cancel.ThrowIfCancellationRequested();
                GetDirStats(dir, data, cancel);
            }
        }
    }
}
