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
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Options;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Abstractions.Sync;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Protocol.Deltas;
using Lib.P2P;

namespace Catalyst.Core.Modules.Sync
{
    public class Synchronizer : ISynchronizer
    {
        public SyncState SyncState { get; }
        private bool _disposed;
        private readonly long _rangeSize;
        private readonly IUserOutput _userOutput;
        private readonly ILedger _ledger;
        private readonly IDeltaDfsReader _deltaDfsReader;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly IMapperProvider _mapperProvider;
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IPeerSyncManager _peerSyncManager;
        private readonly IDeltaHeightWatcher _deltaHeightWatcher;
        private readonly IDfsService _dfsService;
        private readonly IHashProvider _hashProvider;

        private readonly IDeltaCache _deltaCache;

        private Cid _previousHash;

        private Task _syncDeltaIndexTask;

        public long CurrentHighestDeltaIndexStored => _deltaIndexService.Height();

        public IObservable<long> SyncCompleted { get; }
        private readonly ReplaySubject<long> _syncCompletedReplaySubject;

        public Synchronizer(SyncState syncState,
            IPeerSyncManager peerSyncManager,
            IDeltaCache deltaCache,
            IDeltaHeightWatcher deltaHeightWatcher,
            ILedger ledger,
            IDeltaHashProvider deltaHashProvider,
            IDeltaDfsReader deltaDfsReader,
            IDeltaIndexService deltaIndexService,
            IDfsService dfsService,
            IHashProvider hashProvider,
            IMapperProvider mapperProvider,
            IUserOutput userOutput,
            long rangeSize = 20, //cannot go over 20 until udp network fragmentation is fixed
            IScheduler scheduler = null)
        {
            SyncState = syncState;
            _peerSyncManager = peerSyncManager;
            _deltaHeightWatcher = deltaHeightWatcher;
            _deltaCache = deltaCache;
            _rangeSize = rangeSize;
            _ledger = ledger;
            _deltaDfsReader = deltaDfsReader;
            _deltaIndexService = deltaIndexService;
            _mapperProvider = mapperProvider;
            _userOutput = userOutput;

            _deltaHashProvider = deltaHashProvider;

            _dfsService = dfsService;
            _hashProvider = hashProvider;

            _syncCompletedReplaySubject = new ReplaySubject<long>(1, scheduler ?? Scheduler.Default);
            SyncCompleted = _syncCompletedReplaySubject.AsObservable();

            _previousHash = _deltaIndexService.LatestDeltaIndex().Cid;
            //_previousHash = deltaCache.GenesisHash;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (SyncState.IsRunning)
            {
                _userOutput.WriteLine("Sync is already running.");
                return;
            }

            SyncState.IsRunning = true;
            _userOutput.WriteLine("Starting Sync...");

            _deltaHeightWatcher.Start();

            //if (_peerSyncManager.ContainsPeerHistory())
            //var test = new CancellationTokenSource();
            //test.CancelAfter(TimeSpan.FromSeconds(5));test.Token

            await _peerSyncManager.WaitForPeersAsync(cancellationToken).ConfigureAwait(false);

            //if (test.Token.IsCancellationRequested)
            //{
            //    SyncState.IsSynchronized = true;
            //    return;
            //}

            //await _deltaHeightWatcher.WaitForDeltaHeightAsync(cancellationToken);
            var highestDeltaIndex = await _deltaHeightWatcher.GetHighestDeltaIndexAsync();
            if (highestDeltaIndex == null || highestDeltaIndex.Height <= CurrentHighestDeltaIndexStored)
            {
                await Completed().ConfigureAwait(false);
                return;
            }

            _currentSyncIndex = CurrentHighestDeltaIndexStored;

            _peerSyncManager.Start();

            _syncDeltaIndexTask = Task.Factory.StartNew(SyncDeltaIndexes, TaskCreationOptions.LongRunning, cancellationToken);

            Progress(_currentSyncIndex, _rangeSize);
        }

        private long _currentSyncIndex;
        private long _previousDeltaWatcherHeight;

        private async Task SyncDeltaIndexes(object state)
        {
            while (!SyncState.IsSynchronized)
            {
                ProcessDeltaIndexRange(_peerSyncManager.DeltaHistoryOutputQueue.Take());
            }
        }

        private void ProcessDeltaIndexRange(IEnumerable<DeltaIndex> deltaIndexRange)
        {
            var deltaIndexRangeDao = deltaIndexRange.Select(x =>
                DeltaIndexDao.ToDao<DeltaIndex>(x, _mapperProvider)).ToList();

            var firstDeltaIndex = deltaIndexRangeDao.FirstOrDefault();
            if (firstDeltaIndex == null || firstDeltaIndex.Cid != _previousHash)
            {
                return;
            }

            deltaIndexRangeDao.Remove(firstDeltaIndex);

            //.Where(x => x.Cid != _deltaCache.GenesisHash)

            DownloadDeltas(deltaIndexRangeDao);
            UpdateState(deltaIndexRangeDao);

            //if (!CheckSyncProgressAsync().ConfigureAwait(false).GetAwaiter().GetResult())
            //{
            //    Progress(_deltaIndexService.Height(), _rangeSize);
            //}

            CheckSyncProgressAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            Progress(_deltaIndexService.Height(), _rangeSize);
        }

        private int GetSyncProgressPercentage()
        {
            var percentageSync = _deltaIndexService.Height() /
                _deltaHeightWatcher.GetHighestDeltaIndexAsync().GetAwaiter().GetResult().Height * 100;
            return (int)percentageSync;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!SyncState.IsRunning)
            {
                _userOutput.WriteLine("Sync is not currently running.");
                return;
            }

            _deltaHeightWatcher.Stop();
            _peerSyncManager.Stop();

            _userOutput.WriteLine("Sync has been stopped");

            SyncState.IsRunning = false;
        }

        private void Progress(long index, long range)
        {
            if (!SyncState.IsSynchronized)
            {
                _peerSyncManager.GetDeltaIndexRangeFromPeers(index, range);
            }
        }

        private void DownloadDeltas(IList<DeltaIndexDao> deltaIndexes)
        {
            Parallel.ForEach(deltaIndexes, async deltaIndex =>
            {
                while (true)
                {
                    try
                    {
                        var deltaStream =
                            await _dfsService.UnixFsApi.ReadFileAsync(deltaIndex.Cid).ConfigureAwait(false);
                        await _dfsService.UnixFsApi
                           .AddAsync(deltaStream, options: new AddFileOptions { Hash = _hashProvider.HashingAlgorithm.Name })
                           .ConfigureAwait(false);
                        return;
                    }
                    catch (Exception exc) { }

                    await Task.Delay(100).ConfigureAwait(false);
                }
            });
        }

        private void UpdateState(List<DeltaIndexDao> deltaIndexes) => deltaIndexes.ForEach(x =>
        {
            if (_deltaHashProvider.TryUpdateLatestHash(_previousHash, x.Cid))
            {
                _previousHash = x.Cid;
            }
        });

        private async Task<bool> CheckSyncProgressAsync()
        {
            var highestDeltaIndex = await _deltaHeightWatcher.GetHighestDeltaIndexAsync();
                //await FinalizeSyncToEndAsync().ConfigureAwait(false);
            if (CurrentHighestDeltaIndexStored >= highestDeltaIndex.Height)
            {
                _userOutput.WriteLine($"Sync Progress: {GetSyncProgressPercentage()}%");
                await Completed().ConfigureAwait(false);
                return true;
            }

            _userOutput.WriteLine($"Sync Progress: {GetSyncProgressPercentage()}%");
            return false;
        }

        private async Task<DeltaIndex> FinalizeSyncToEndAsync()
        {
            var highestDeltaIndex = await _deltaHeightWatcher.GetHighestDeltaIndexAsync().ConfigureAwait(false);
            while (highestDeltaIndex.Height > CurrentHighestDeltaIndexStored && highestDeltaIndex.Height < CurrentHighestDeltaIndexStored + _rangeSize)
            {
                var cid = highestDeltaIndex.Cid.ToArray().ToCid();

                _deltaHashProvider.TryUpdateLatestHash(_previousHash, cid);
                _previousHash = cid;

                highestDeltaIndex = await _deltaHeightWatcher.GetHighestDeltaIndexAsync().ConfigureAwait(false);
            }
            return highestDeltaIndex;
        }

        private async Task Completed()
        {
            SyncState.IsSynchronized = true;
            _syncCompletedReplaySubject.OnNext(CurrentHighestDeltaIndexStored);
            await StopAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //_syncDeltaIndexTask?.Dispose();
                    _peerSyncManager?.Dispose();
                    _deltaHeightWatcher?.Dispose();
                }
            }
            _disposed = true;
        }
    }
}
