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
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Abstractions.Sync;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Protocol.Deltas;
using Serilog;
using Lib.P2P;

namespace Catalyst.Core.Modules.Sync
{
    public class Synchroniser : ISynchroniser
    {
        public SyncState State { set; get; }
        public bool IsRunning { private set; get; }
        private bool _disposed;
        private readonly int _rangeSize;
        private readonly IUserOutput _userOutput;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly IMapperProvider _mapperProvider;
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IPeerSyncManager _peerSyncManager;
        private readonly IDeltaHeightWatcher _deltaHeightWatcher;
        private readonly ILogger _logger;

        private IDisposable _scoredDeltaIndexRangeDisposable;

        private Cid _previousHash;

        public ulong CurrentHighestDeltaIndexStored => _deltaIndexService.Height();
        public IDeltaCache DeltaCache { get; }

        public IObservable<ulong> SyncCompleted { get; }
        private readonly ReplaySubject<ulong> _syncCompletedReplaySubject;

        private readonly int _peerDiscoveryDelay;
        private readonly int _deltaHeightDelay;

        public Synchroniser(SyncState syncState,
            IPeerSyncManager peerSyncManager,
            IDeltaCache deltaCache,
            IDeltaHeightWatcher deltaHeightWatcher,
            IDeltaHashProvider deltaHashProvider,
            IDeltaDfsReader deltaDfsReader,
            IDeltaIndexService deltaIndexService,
            IMapperProvider mapperProvider,
            IUserOutput userOutput,
            ILogger logger,
            int rangeSize = 100,
            int peerDiscoveryDelay = 10,
            int deltaHeightDelay = 30,
            IScheduler scheduler = null)
        {
            State = syncState;
            _peerSyncManager = peerSyncManager;
            _deltaHeightWatcher = deltaHeightWatcher;
            DeltaCache = deltaCache;
            _rangeSize = rangeSize;
            _deltaIndexService = deltaIndexService;
            _mapperProvider = mapperProvider;
            _userOutput = userOutput;

            _deltaHashProvider = deltaHashProvider;

            _logger = logger;

            _syncCompletedReplaySubject = new ReplaySubject<ulong>(1, scheduler ?? Scheduler.Default);
            SyncCompleted = _syncCompletedReplaySubject.AsObservable();

            _peerDiscoveryDelay = peerDiscoveryDelay;
            _deltaHeightDelay = deltaHeightDelay;
        }

        public void UpdateState(ulong _latestKnownDeltaNumber)
        {
            State.CurrentBlock = _latestKnownDeltaNumber;
        }

        /// <inheritdoc />
        public IEnumerable<Cid> CacheDeltasBetween(Cid latestKnownDeltaHash,
            Cid targetDeltaHash,
            CancellationToken cancellationToken)
        {
            var thisHash = targetDeltaHash;

            do
            {
                if (!DeltaCache.TryGetOrAddConfirmedDelta(thisHash, out var retrievedDelta, cancellationToken))
                {
                    yield break;
                }

                var previousDfsHash = retrievedDelta.PreviousDeltaDfsHash.ToByteArray().ToCid();

                _logger.Debug("Retrieved delta {previous} as predecessor of {current}",
                    previousDfsHash, thisHash);

                yield return thisHash;

                thisHash = previousDfsHash;
            } while (!thisHash.Equals(latestKnownDeltaHash)
             && !cancellationToken.IsCancellationRequested);

            yield return thisHash;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (IsRunning)
            {
                _userOutput.WriteLine("Sync is already running.");
                return;
            }

            IsRunning = true;
            _previousHash = _deltaIndexService.LatestDeltaIndex().Cid;
            _userOutput.WriteLine("Starting Sync...");

            _scoredDeltaIndexRangeDisposable = _peerSyncManager.ScoredDeltaIndexRange.Subscribe(ProcessDeltaIndexRange);

            _deltaHeightWatcher.Start();

            using (var WaitForPeersCancellationSource = new CancellationTokenSource())
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, WaitForPeersCancellationSource.Token);
                cts.CancelAfter(TimeSpan.FromSeconds(_peerDiscoveryDelay));
                await _peerSyncManager.WaitForPeersAsync(cts.Token).ConfigureAwait(false);
            }

            var highestDeltaIndex = await _deltaHeightWatcher.WaitForDeltaIndexAsync(TimeSpan.FromSeconds(_deltaHeightDelay), cancellationToken).ConfigureAwait(false);
            if (highestDeltaIndex == null || highestDeltaIndex.Height <= CurrentHighestDeltaIndexStored)
            {
                await Completed().ConfigureAwait(false);
                return;
            }

            State.CurrentBlock = State.StartingBlock = CurrentHighestDeltaIndexStored;
            State.HighestBlock = highestDeltaIndex.Height;

            _peerSyncManager.Start();

            Progress(CurrentHighestDeltaIndexStored, _rangeSize);
        }

        private void ProcessDeltaIndexRange(IEnumerable<DeltaIndex> deltaIndexRange)
        {
            var deltaIndexRangeDao = deltaIndexRange.Select(x =>
                DeltaIndexDao.ToDao<DeltaIndex>(x, _mapperProvider)).ToList();

            var firstDeltaIndex = deltaIndexRangeDao.FirstOrDefault();
            if (firstDeltaIndex == null || firstDeltaIndex.Cid != _previousHash)
            {
                _logger.Error($"Sync Error - Previous delta({_previousHash}) does not match next delta({firstDeltaIndex.Cid})");
                return;
            }

            deltaIndexRangeDao.Remove(firstDeltaIndex);

            DownloadDeltas(deltaIndexRangeDao);
            UpdateState(deltaIndexRangeDao);

            CheckSyncProgressAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            Progress(CurrentHighestDeltaIndexStored, _rangeSize);
        }

        private void DownloadDeltas(IList<DeltaIndexDao> deltaIndexes)
        {
            Parallel.ForEach(deltaIndexes, async deltaIndex =>
            {
                while (true)
                {
                    try
                    {
                        if (DeltaCache.TryGetOrAddConfirmedDelta(deltaIndex.Cid, out Delta _))
                        {
                            break;
                        }
                    }
                    catch (Exception exc) { }

                    await Task.Delay(100).ConfigureAwait(false);
                }
            });
        }

        private async Task<int> GetSyncProgressPercentageAsync()
        {
            var currentDeltaIndex = await _deltaHeightWatcher.GetHighestDeltaIndexAsync();
            var percentageSync = _deltaIndexService.Height() / currentDeltaIndex.Height * 100;
            return (int) percentageSync;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!IsRunning)
            {
                _userOutput.WriteLine("Sync is not currently running.");
                return Task.CompletedTask;
            }

            _deltaHeightWatcher.Stop();
            _peerSyncManager.Stop();

            _userOutput.WriteLine("Sync has been stopped");

            IsRunning = false;

            return Task.CompletedTask;
        }

        private void Progress(ulong index, int range)
        {
            if (!State.IsSynchronized)
            {
                _peerSyncManager.GetDeltaIndexRangeFromPeers(index, range);
            }
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
            State.CurrentBlock = CurrentHighestDeltaIndexStored;
            State.HighestBlock = highestDeltaIndex.Height;

            if (CurrentHighestDeltaIndexStored >= highestDeltaIndex.Height)
            {
                _userOutput.WriteLine($"Sync Progress: { await GetSyncProgressPercentageAsync()}%");
                await Completed().ConfigureAwait(false);
                return true;
            }

            _userOutput.WriteLine($"Sync Progress: {await GetSyncProgressPercentageAsync()}%");
            return false;
        }

        private async Task Completed()
        {
            State.IsSynchronized = true;
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
                    _scoredDeltaIndexRangeDisposable?.Dispose();
                    _peerSyncManager?.Dispose();
                    _deltaHeightWatcher?.Dispose();
                }
            }
            _disposed = true;
        }
    }
}
