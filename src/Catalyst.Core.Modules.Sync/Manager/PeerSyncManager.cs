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
using Catalyst.Core.Modules.Sync.Modal;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Google.Protobuf;

namespace Catalyst.Core.Modules.Sync.Manager
{
    public class PeerSyncManager : IPeerSyncManager
    {
        public int PeerCount { private set; get; } = 5;

        private bool _disposed;
        private readonly IPeerSettings _peerSettings;
        private readonly IPeerClient _peerClient;
        private readonly IPeerRepository _peerRepository;
        private readonly IPeerService _peerService;
        private readonly IUserOutput _userOutput;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly ReplaySubject<IEnumerable<DeltaIndex>> _scoredDeltaIndexRangeSubject;

        private IDisposable _deltaHistorySubscription;
        private Task _syncDeltaIndexesTask;

        public IObservable<IEnumerable<DeltaIndex>> ScoredDeltaIndexRange { get; }

        //private readonly ConcurrentDictionary<int, DeltaIndexSyncItem> _deltaIndexSyncPool;

        public int MaxSyncPoolSize { get; } = 1;

        public IDictionary<int, DeltaHistoryRanker> _deltaHistoryRankers;

        public BlockingCollection<DeltaHistoryRanker> DeltaHistoryInputQueue { private set; get; }
        public BlockingCollection<DeltaHistoryRanker> DeltaHistoryOutputQueue { private set; get; }

        //public bool IsPoolAvailable() { return MaxSyncPoolSize - _deltaIndexSyncPool.Count() > 0; }

        public PeerSyncManager(IPeerSettings peerSettings,
            IPeerClient peerClient,
            IPeerRepository peerRepository,
            IPeerService peerService,
            IUserOutput userOutput,
            IDeltaIndexService deltaIndexService,
            IScheduler scheduler = null)
        {
            _peerSettings = peerSettings;
            _peerClient = peerClient;
            _peerRepository = peerRepository;
            _peerService = peerService;
            _userOutput = userOutput;
            _deltaIndexService = deltaIndexService;

            _deltaHistoryRankers = new ConcurrentDictionary<int, DeltaHistoryRanker>();

            _scoredDeltaIndexRangeSubject =
                new ReplaySubject<IEnumerable<DeltaIndex>>(1, scheduler ?? Scheduler.Default);
            ScoredDeltaIndexRange = _scoredDeltaIndexRangeSubject.AsObservable();

            DeltaHistoryInputQueue = new BlockingCollection<DeltaHistoryRanker>();
            DeltaHistoryOutputQueue = new BlockingCollection<DeltaHistoryRanker>();

            //_deltaIndexSyncPool = new ConcurrentDictionary<int, DeltaIndexSyncItem>();
        }

        public bool PeersAvailable()
        {
            PeerCount = Math.Min(_peerRepository.GetActivePeers(PeerCount).Count(), 5);
            return PeerCount > 0;
        }

        public bool ContainsPeerHistory() { return _peerRepository.GetAll().Any(); }

        public async Task WaitForPeersAsync(CancellationToken cancellationToken = default)
        {
            while (!PeersAvailable() && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }

        public void GetDeltaIndexRangeFromPeers(int index, int range)
        {
            var deltaHistoryRequest = new DeltaHistoryRequest
            { Height = (uint)index, Range = (uint)range };

            //var deltaIndexRange = new DeltaIndexSyncItem
            //{
            //    Request = deltaHistoryRequest,
            //    DeltaIndexRangeRanked = new List<DeltaIndexScore>()
            //};

            _deltaHistoryRankers.Add(index, new DeltaHistoryRanker());

            //_deltaIndexSyncPool.TryAdd(index, deltaIndexRange);

            SendMessageToRandomPeers(deltaHistoryRequest);
        }

        private void SendMessageToRandomPeers(IMessage message) => SendMessageToPeers(message, _peerRepository.GetActivePeers(PeerCount).Select(x=>x.PeerId));

        public void SendMessageToPeers(IMessage message, IEnumerable<PeerId> peers)
        {
            var protocolMessage = message.ToProtocolMessage(_peerSettings.PeerId);
            foreach (var peer in peers)
            {
                _peerClient.SendMessage(new MessageDto(
                    protocolMessage,
                    peer));
            }
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
                var deltaHistoryRanker = DeltaHistoryInputQueue.Take();
                
            }

            //while (true)
            //{
            //    foreach (var key in _deltaIndexSyncPool.Keys)
            //    {
            //        var deltaIndexSyncItem = _deltaIndexSyncPool[key];
            //        var combinedScores = deltaIndexSyncItem.DeltaIndexRangeRanked.Select(x => x.Score).Sum();

            //        var lastUpdatedOffset = deltaIndexSyncItem.LastUpdated - DateTime.UtcNow;
            //        if (lastUpdatedOffset > TimeSpan.FromSeconds(5))
            //        {
            //            if (combinedScores <= 0)
            //            {
            //                SendMessageToRandomPeers(deltaIndexSyncItem.Request);
            //            }
            //        }

            //        if (combinedScores < PeerCount * 0.5d)
            //        {
            //            continue;
            //        }

            //        var deltaIndexScoredByDescendingOrder =
            //            deltaIndexSyncItem.DeltaIndexRangeRanked.OrderByDescending(x => x.Score);
            //        var highestScoreDeltaIndexRange = deltaIndexScoredByDescendingOrder.First();
            //        var accuracyPercentage = highestScoreDeltaIndexRange.Score / (double)combinedScores * 100d;
            //        if (!(accuracyPercentage >= 99))
            //        {
            //            continue;
            //        }

            //        deltaIndexSyncItem.DeltaIndexRangeRanked.RemoveAll(x => x != highestScoreDeltaIndexRange);

            //        deltaIndexSyncItem.Complete = true;

            //        if (key == _deltaIndexSyncPool.Keys.Min() && _deltaIndexSyncPool.TryRemove(key, out _))
            //        {
            //            _scoredDeltaIndexRangeSubject.OnNext(highestScoreDeltaIndexRange.DeltaIndexes);
            //        }
            //    }

            //    await Task.Delay(100);
            //}
        }

        private void DeltaHistoryOnNext(DeltaHistoryResponse deltaHistoryResponse)
        {
            if (deltaHistoryResponse.DeltaIndex.Count == 0)
            {
                return;
            }

            var startHeight = (int)deltaHistoryResponse.DeltaIndex.First().Height;
            _deltaHistoryRankers[startHeight].Add(deltaHistoryResponse.DeltaIndex);

            //var startHeight = deltaHistoryResponse.DeltaIndex.First().Height;
            //var newDeltaIndexScores = new DeltaIndexScore { DeltaIndexes = deltaHistoryResponse.DeltaIndex, Score = 1 };
            //if (!_deltaIndexSyncPool.TryGetValue((int)startHeight, out var deltaIndexScores))
            //{
            //    return;
            //}

            //var exists =
            //    deltaIndexScores.DeltaIndexRangeRanked.FirstOrDefault(x =>
            //        x.DeltaIndexes.Equals(deltaHistoryResponse.DeltaIndex));
            //if (exists != null)
            //{
            //    exists.Score++;
            //}
            //else
            //{
            //    deltaIndexScores.DeltaIndexRangeRanked.Add(newDeltaIndexScores);
            //}

            //deltaIndexScores.LastUpdated = DateTime.UtcNow;
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
