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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Models;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Sync.Extensions;
using Catalyst.Core.Modules.Sync.Modal;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace Catalyst.Core.Modules.Sync.Manager
{
    public class PeerSyncManager : IPeerSyncManager
    {
        public int PeerCount { private set; get; } = 5;

        private bool _disposed;
        private readonly double _threshold;
        private readonly IMessenger _messenger;
        private readonly IPeerRepository _peerRepository;
        private readonly IPeerService _peerService;
        private readonly IUserOutput _userOutput;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly ReplaySubject<IEnumerable<DeltaIndex>> _scoredDeltaIndexRangeSubject;

        private IDisposable _deltaHistorySubscription;
        private Task _syncDeltaIndexesTask;

        public IObservable<IEnumerable<DeltaIndex>> ScoredDeltaIndexRange { get; }

        public int MaxSyncPoolSize { get; } = 1;

        public IDictionary<long, DeltaHistoryRanker> _deltaHistoryRankers;

        public BlockingCollection<DeltaHistoryRanker> DeltaHistoryInputQueue { private set; get; }
        public BlockingCollection<RepeatedField<DeltaIndex>> DeltaHistoryOutputQueue { private set; get; }

        private readonly IDeltaHeightWatcher _deltaHeightWatcher;

        public PeerSyncManager(IMessenger messenger,
            IPeerRepository peerRepository,
            IPeerService peerService,
            IUserOutput userOutput,
            IDeltaIndexService deltaIndexService,
            IDeltaHeightWatcher deltaHeightWatcher,
            double threshold = 0.7d,
            IScheduler scheduler = null)
        {
            _messenger = messenger;
            _peerRepository = peerRepository;
            _peerService = peerService;
            _userOutput = userOutput;
            _deltaIndexService = deltaIndexService;
            _deltaHeightWatcher = deltaHeightWatcher;

            _deltaHistoryRankers = new ConcurrentDictionary<long, DeltaHistoryRanker>();

            _scoredDeltaIndexRangeSubject =
                new ReplaySubject<IEnumerable<DeltaIndex>>(1, scheduler ?? Scheduler.Default);
            ScoredDeltaIndexRange = _scoredDeltaIndexRangeSubject.AsObservable();

            DeltaHistoryInputQueue = new BlockingCollection<DeltaHistoryRanker>();
            DeltaHistoryOutputQueue = new BlockingCollection<RepeatedField<DeltaIndex>>();

            _threshold = threshold;
        }

        public bool PeersAvailable()
        {
            //PeerCount = Math.Min(_peerRepository.GetActivePeers(PeerCount).Count(), 5);
            //return PeerCount > 0;
            //PeerCount = Math.Min(_peerRepository.GetActivePeers(PeerCount).Count(), 5);
            return _peerRepository.Count() > 0;
        }

        public bool ContainsPeerHistory() { return _peerRepository.GetAll().Any(); }

        public async Task WaitForPeersAsync(CancellationToken cancellationToken = default)
        {
            while (!PeersAvailable() && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }

        public void GetDeltaIndexRangeFromPeers(long index, long range)
        {
            var deltaHistoryRequest = new DeltaHistoryRequest
            { Height = (uint)index, Range = (uint)range };

            _deltaHistoryRankers.Add(index, new DeltaHistoryRanker(deltaHistoryRequest));
        }

        public void Start()
        {
            _deltaHistorySubscription = _peerService.MessageStream.Where(x => x.Payload?.TypeUrl != null &&
                    x.Payload.TypeUrl.EndsWith(typeof(DeltaHistoryResponse).ShortenedProtoFullName()))
               .Select(x => x.Payload.FromProtocolMessage<DeltaHistoryResponse>()).Subscribe(DeltaHistoryOnNext);

            _syncDeltaIndexesTask = Task.Factory.StartNew(SyncDeltaIndexes);
        }

        public void Stop() => Dispose();

        private async Task SyncDeltaIndexes()
        {
            while (true)
            {
                var peers = _deltaHeightWatcher.DeltaHeightRanker.GetPeers();
                foreach (var key in _deltaHistoryRankers.Keys)
                {
                    var first = _deltaHistoryRankers.Keys.First() == key;
                    var messageCount = Math.Min(peers.Count(), 50);
                    var minimumThreshold = messageCount * _threshold;
                    var deltaHistoryRanker = _deltaHistoryRankers[key];
                    var score = deltaHistoryRanker.GetHighestScore();
                    if (score > minimumThreshold && first)
                    {
                        DeltaHistoryOutputQueue.Add(deltaHistoryRanker.GetMostPopular());
                        _deltaHistoryRankers.Remove(key);
                    }
                    _messenger.SendMessageToPeers(deltaHistoryRanker.DeltaHistoryRequest, peers);
                }

                await Task.Delay(1000);
            }
        }

        private void DeltaHistoryOnNext(DeltaHistoryResponse deltaHistoryResponse)
        {
            if (deltaHistoryResponse.DeltaIndex.Count == 0)
            {
                return;
            }

            var startHeight = (int)deltaHistoryResponse.DeltaIndex.First().Height;
            if (_deltaHistoryRankers.ContainsKey(startHeight))
            {
                _deltaHistoryRankers[startHeight].Add(deltaHistoryResponse.DeltaIndex);
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
                    _syncDeltaIndexesTask?.Dispose();
                }
            }
            _disposed = true;
        }
    }
}
