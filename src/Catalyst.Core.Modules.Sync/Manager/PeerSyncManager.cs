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
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;

namespace Catalyst.Core.Modules.Sync.Manager
{
    public class PeerSyncManager : IPeerSyncManager
    {
        private bool _isRunning;
        private bool _disposed;
        private readonly double _threshold;
        private int _minimumPeers;
        private readonly IPeerClient _peerClient;
        private readonly IPeerRepository _peerRepository;
        private readonly IPeerService _peerService;
        private readonly IUserOutput _userOutput;
        private readonly IDfsService _dfsService;
        private readonly ReplaySubject<IEnumerable<DeltaIndex>> _scoredDeltaIndexRangeSubject;

        private IDisposable _deltaHistorySubscription;

        public IObservable<IEnumerable<DeltaIndex>> ScoredDeltaIndexRange { get; }

        public int MaxSyncPoolSize { get; } = 1;

        public DeltaHistoryRanker _deltaHistoryRanker;


        private readonly IDeltaHeightWatcher _deltaHeightWatcher;

        public PeerSyncManager(IPeerClient peerClient,
            IPeerRepository peerRepository,
            IPeerService peerService,
            IUserOutput userOutput,
            IDeltaHeightWatcher deltaHeightWatcher,
            IDfsService dfsService,
            double threshold = 0.7d,
            int minimumPeers = 2,
            IScheduler scheduler = null)
        {
            _peerClient = peerClient;
            _peerRepository = peerRepository;
            _peerService = peerService;
            _userOutput = userOutput;
            _deltaHeightWatcher = deltaHeightWatcher;
            _dfsService = dfsService;
            _scoredDeltaIndexRangeSubject =
                new ReplaySubject<IEnumerable<DeltaIndex>>(1, scheduler ?? Scheduler.Default);
            ScoredDeltaIndexRange = _scoredDeltaIndexRangeSubject.AsObservable();

            _threshold = threshold;
            _minimumPeers = minimumPeers;
        }

        public bool PeersAvailable()
        {
            return _dfsService.SwarmApi.PeersAsync().ConfigureAwait(false).GetAwaiter().GetResult().Count() >= _minimumPeers;
        }

        public bool ContainsPeerHistory() { return _peerRepository.GetAll().Any(); }

        public async Task WaitForPeersAsync(CancellationToken cancellationToken = default)
        {
            while (!PeersAvailable() && !cancellationToken.IsCancellationRequested)
            {
                _userOutput.WriteLine("Waiting for peers..");
                await Task.Delay(1000, cancellationToken);
            }
        }

        public void GetDeltaIndexRangeFromPeers(ulong index, int range)
        {
            var deltaHistoryRequest = new DeltaHistoryRequest
            { Height = (uint)index, Range = (uint)range };

            _deltaHistoryRanker = new DeltaHistoryRanker(deltaHistoryRequest);
        }

        public void Start()
        {
            _isRunning = true;
            _deltaHistorySubscription = _peerService.MessageStream.Where(x => x.Payload?.TypeUrl != null &&
                    x.Payload.TypeUrl.EndsWith(typeof(DeltaHistoryResponse).ShortenedProtoFullName()))
               .Select(x => x.Payload.FromProtocolMessage<DeltaHistoryResponse>()).Subscribe(DeltaHistoryOnNext);

            Task.Factory.StartNew(SyncDeltaIndexes);
        }

        public void Stop() => Dispose();

        private async Task SyncDeltaIndexes()
        {
            while (_isRunning)
            {
                if (_deltaHistoryRanker != null)
                {
                    var peers = _deltaHeightWatcher.DeltaHeightRanker.GetPeers();
                    var messageCount = Math.Min(_minimumPeers, 50);
                    var minimumThreshold = messageCount * _threshold;
                    var score = _deltaHistoryRanker.GetHighestScore();
                    if (score > minimumThreshold)
                    {
                        var deltaIndexes = _deltaHistoryRanker.GetMostPopular();
                        _deltaHistoryRanker = null;
                        _scoredDeltaIndexRangeSubject.OnNext(deltaIndexes);
                        continue;
                    }
                    _peerClient.SendMessageToPeers(_deltaHistoryRanker.DeltaHistoryRequest, peers);
                }
                await Task.Delay(2000);
            }
        }

        private void DeltaHistoryOnNext(DeltaHistoryResponse deltaHistoryResponse)
        {
            //First block is always previous block
            if (deltaHistoryResponse.DeltaIndex.Count == 1)
            {
                return;
            }

            var startHeight = (int)deltaHistoryResponse.DeltaIndex.FirstOrDefault()?.Height;
            if (_deltaHistoryRanker != null && startHeight == _deltaHistoryRanker.Height)
            {
                _deltaHistoryRanker.Add(deltaHistoryResponse.DeltaIndex);
            }
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
                    _deltaHistorySubscription?.Dispose();
                }
            }
            _disposed = true;
            _isRunning = false;
        }
    }
}
