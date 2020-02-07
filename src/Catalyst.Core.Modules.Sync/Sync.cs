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
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Sync.Modal;
using Catalyst.Protocol.Deltas;
using Google.Protobuf.Collections;
using Lib.P2P;
using Polly;
using Polly.Retry;

namespace Catalyst.Core.Modules.Sync
{
    public class Sync
    {
        public SyncState SyncState { get; }
        private readonly int _rangeSize;
        private readonly IUserOutput _userOutput;
        private readonly ILedger _ledger;
        private readonly IDeltaDfsReader _deltaDfsReader;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly IMapperProvider _mapperProvider;

        private readonly IPeerSyncManager _peerSyncManager;
        private readonly IDeltaHeightWatcher _deltaHeightWatcher;
        private readonly IDfsService _dfsService;

        private readonly AsyncRetryPolicy _retryPolicy;

        public int CurrentDeltaIndex { private set; get; }
        public int MaxDeltaIndexStored => _deltaIndexService.Height();

        public IObservable<int> SyncCompleted { get; }
        private readonly ReplaySubject<int> _syncCompletedReplaySubject;

        public Sync(SyncState syncState,
            IPeerSyncManager peerSyncManager,
            IDeltaHeightWatcher deltaHeightWatcher,
            ILedger ledger,
            IDeltaDfsReader deltaDfsReader,
            IDeltaIndexService deltaIndexService,
            IDfsService dfsService,
            IMapperProvider mapperProvider,
            IUserOutput userOutput,
            int rangeSize = 20,
            int syncTaskPoolLimit = 1,
            IScheduler scheduler = null)
        {
            _retryPolicy = Policy.Handle<Exception>()
               .WaitAndRetryAsync(10, x => TimeSpan.FromMilliseconds(100));

            SyncState = syncState;
            _peerSyncManager = peerSyncManager;
            _deltaHeightWatcher = deltaHeightWatcher;

            _rangeSize = rangeSize;
            _ledger = ledger;
            _deltaDfsReader = deltaDfsReader;
            _deltaIndexService = deltaIndexService;
            _mapperProvider = mapperProvider;
            _userOutput = userOutput;

            _dfsService = dfsService;

            _syncCompletedReplaySubject = new ReplaySubject<int>(1, scheduler ?? Scheduler.Default);
            SyncCompleted = _syncCompletedReplaySubject.AsObservable();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            SyncState.IsRunning = true;
            _userOutput.WriteLine("Starting Sync...");

            await _deltaHeightWatcher.StartAsync(cancellationToken).ConfigureAwait(false);

            //if (_peerSyncManager.ContainsPeerHistory())
            //var test = new CancellationTokenSource();
            //test.CancelAfter(TimeSpan.FromSeconds(5));test.Token

            await _peerSyncManager.WaitForPeersAsync().ConfigureAwait(false);

            //if (test.Token.IsCancellationRequested)
            //{
            //    SyncState.IsSynchronized = true;
            //    return;
            //}

            await _deltaHeightWatcher.WaitForDeltaHeightAsync(MaxDeltaIndexStored, cancellationToken);

            if (_deltaHeightWatcher.LatestDeltaHash.Height <= MaxDeltaIndexStored)
            {
                SyncState.IsSynchronized = true;
                return;
            }

            _peerSyncManager.ScoredDeltaIndexRange.Subscribe(OnNextScoredDeltaIndexRange);
            _currentSyncIndex = MaxDeltaIndexStored;

            var syncDeltaIndexTask = Task.Factory.StartNew(SyncDeltaIndexes, cancellationToken);
            await _peerSyncManager.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        private int _currentSyncIndex;

        private async Task SyncDeltaIndexes()
        {
            while (SyncState.IsRunning)
            {
                _peerSyncManager.GetDeltaHeight();
                if (_peerSyncManager.IsPoolAvailable())
                {
                    if (_deltaHeightWatcher.LatestDeltaHash.Height > _currentSyncIndex)
                    {
                        await ProgressAsync(_currentSyncIndex, _rangeSize).ConfigureAwait(false);
                        _currentSyncIndex += _rangeSize;
                    }
                }

                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        private void OnNextScoredDeltaIndexRange(RepeatedField<DeltaIndex> deltaIndexRange)
        {
            var deltaIndexRangeDao = deltaIndexRange.Select(x =>
                x.ToDao<DeltaIndex, DeltaIndexDao>(_mapperProvider)).ToList();

            DownloadDeltas(deltaIndexRangeDao);
            UpdateState(deltaIndexRangeDao);

            var percentageSync = _deltaIndexService.Height() /
                (decimal) _deltaHeightWatcher.LatestDeltaHash.Height * 100;
            _userOutput.WriteLine($"Sync Progress: {percentageSync}%");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!SyncState.IsRunning)
            {
                _userOutput.WriteLine("Sync is not currently running.");
                return;
            }

            _userOutput.WriteLine("Sync has been signaled to stop");

            await _deltaHeightWatcher.StopAsync(cancellationToken).ConfigureAwait(false);

            _userOutput.WriteLine("Sync has been stopped");

            SyncState.IsRunning = false;
        }

        private async Task ProgressAsync(int index, int range)
        {
            if (!SyncState.IsSynchronized)
            {
                //if (!_peerSyncManager.PeersAvailable())
                //{
                //    IsSynchronized = true;
                //}

                _peerSyncManager.GetDeltaIndexRangeFromPeers(index, range);
            }
        }

        private void DownloadDeltas(IList<DeltaIndexDao> deltaIndexes)
        {
            Parallel.ForEach(deltaIndexes, async deltaIndex =>
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (deltaIndex.Cid == "bafk2bzacecji5gcdd6lxsoazgnbg46c3vttjwwkptiw27enachziizhhkir2w")
                    {
                        return;
                    }

                    if (!_deltaDfsReader.TryReadDeltaFromDfs(deltaIndex.Cid, out _))
                    {
                        throw new Exception(
                            $"Delta: Cid: {deltaIndex.Cid} Hash: {Cid.Decode(deltaIndex.Cid).Hash.ToBase32()} not found in Dfs at height: {deltaIndex.Height}");
                    }

                    await _dfsService.PinApi.AddAsync(deltaIndex.Cid).ConfigureAwait(false);
                });
            });
        }

        private void UpdateState(IEnumerable<DeltaIndexDao> deltaIndexes)
        {
            foreach (var deltaIndex in deltaIndexes)
            {
                _ledger.Update(deltaIndex.Cid);
                CurrentDeltaIndex = deltaIndex.Height;
            }

            if (MaxDeltaIndexStored >= _deltaHeightWatcher.LatestDeltaHash.Height)
            {
                SyncState.IsSynchronized = true;
                _syncCompletedReplaySubject.OnNext(CurrentDeltaIndex);
            }
        }
    }
}
