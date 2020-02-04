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
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Service;
using Catalyst.Protocol.Deltas;
using Google.Protobuf.Collections;

namespace Catalyst.Core.Modules.Sync
{
    public class Sync
    {
        private readonly int _rangeSize;
        private readonly int _syncTaskPoolLimit;
        private readonly IUserOutput _userOutput;
        private readonly ILedger _ledger;
        private readonly IDeltaDfsReader _deltaDfsReader;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly IMapperProvider _mapperProvider;

        private readonly IPeerSyncManager _peerSyncManager;
        private readonly IDeltaHeightWatcher _deltaHeightWatcher;
        private Timer _timer;

        public int CurrentDeltaIndex { private set; get; }
        public int MaxDeltaIndexStored => _deltaIndexService.Height();
        public bool IsSynchronized { private set; get; }
        public bool IsRunning { private set; get; }

        public IObservable<int> SyncCompleted { get; }
        private readonly ReplaySubject<int> _syncCompletedReplaySubject;

        public Sync(IPeerSyncManager peerSyncManager,
            IDeltaHeightWatcher deltaHeightWatcher,
            ILedger ledger,
            IDeltaDfsReader deltaDfsReader,
            IDeltaIndexService deltaIndexService,
            IMapperProvider mapperProvider,
            IUserOutput userOutput,
            int rangeSize = 10,
            int syncTaskPoolLimit = 4,
            IScheduler scheduler = null)
        {
            _peerSyncManager = peerSyncManager;
            _deltaHeightWatcher = deltaHeightWatcher;

            _rangeSize = rangeSize;
            _ledger = ledger;
            _deltaDfsReader = deltaDfsReader;
            _deltaIndexService = deltaIndexService;
            _mapperProvider = mapperProvider;
            _userOutput = userOutput;

            _syncTaskPoolLimit = syncTaskPoolLimit;

            _syncCompletedReplaySubject = new ReplaySubject<int>(1, scheduler ?? Scheduler.Default);
            SyncCompleted = _syncCompletedReplaySubject.AsObservable();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            IsRunning = true;
            _userOutput.WriteLine("Starting Sync...");

            await _deltaHeightWatcher.StartAsync(cancellationToken);
            if (_peerSyncManager.ContainsPeerHistory())
            {
                await _peerSyncManager.WaitForPeersAsync();
                await _deltaHeightWatcher.WaitForDeltaHeightAsync(MaxDeltaIndexStored, cancellationToken);
            }

            _peerSyncManager.ScoredDeltaIndexRange.Subscribe(OnNextScoredDeltaIndexRange);
            if (_deltaHeightWatcher.LatestDeltaHash == null)
            {
                return;
            }

            _currentSyncIndex = MaxDeltaIndexStored;

            var syncDeltaIndexTask = Task.Factory.StartNew(OnSyncDeltaIndex, cancellationToken);
            await _peerSyncManager.StartAsync(cancellationToken);
        }

        private int _currentSyncIndex;

        private async Task OnSyncDeltaIndex()
        {
            while (IsRunning)
            {
                while (_peerSyncManager.IsPoolAvailable())
                {
                    if (_deltaHeightWatcher.LatestDeltaHash.Height <= _currentSyncIndex)
                    {
                        break;
                    }

                    ProgressAsync(_currentSyncIndex, _rangeSize).GetAwaiter().GetResult();
                    _currentSyncIndex += _rangeSize;
                }

                await Task.Delay(100);
            }
        }

        private void OnNextScoredDeltaIndexRange(RepeatedField<DeltaIndex> deltaIndexRange)
        {
            var deltaIndexRangeDao = deltaIndexRange.Select(x =>
                x.ToDao<DeltaIndex, DeltaIndexDao>(_mapperProvider)).ToList();

            DownloadDeltas(deltaIndexRangeDao);
            UpdateIndexes(deltaIndexRangeDao);
            UpdateState(deltaIndexRangeDao);

            var percentageSync = _deltaIndexService.Height() /
                (decimal) _deltaHeightWatcher.LatestDeltaHash.Height * 100;
            _userOutput.WriteLine($"Sync Progress: {percentageSync}%");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!IsRunning)
            {
                _userOutput.WriteLine("Sync is not currently running.");
                return;
            }

            _userOutput.WriteLine("Sync has been signaled to stop");

            await _deltaHeightWatcher.StopAsync(cancellationToken);

            _userOutput.WriteLine("Sync has been stopped");

            IsRunning = false;
        }

        private async Task ProgressAsync(int index, int range)
        {
            if (!IsSynchronized)
            {
                if (!_peerSyncManager.PeersAvailable())
                {
                    IsSynchronized = true;
                }

                _peerSyncManager.GetDeltaIndexRangeFromPeers(index, range);
            }
        }

        private void UpdateIndexes(IEnumerable<DeltaIndexDao> deltaIndexes) { _deltaIndexService.Add(deltaIndexes); }

        private void DownloadDeltas(IEnumerable<DeltaIndexDao> deltaIndexes)
        {
            foreach (var deltaIndex in deltaIndexes)
            {
                if (!_deltaDfsReader.TryReadDeltaFromDfs(deltaIndex.Cid, out _))
                {
                    throw new Exception($"Delta: {deltaIndex.Cid} not found in Dfs at height: {deltaIndex.Height}");
                }
            }
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
                IsSynchronized = true;
                _syncCompletedReplaySubject.OnNext(CurrentDeltaIndex);
            }
        }
    }
}
