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
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Sync.Modal;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace Catalyst.Core.Modules.Sync
{
    public class PeerSyncManager : IPeerSyncManager
    {
        public int PeerCount { get; } = 5;
        private readonly IPeerSettings _peerSettings;
        private readonly IPeerClient _peerClient;
        private readonly IPeerRepository _peerRepository;
        private readonly IPeerService _peerService;
        private readonly IDisposable _deltaHistorySubscription;
        private readonly IUserOutput _userOutput;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly ReplaySubject<RepeatedField<DeltaIndex>> _scoredDeltaIndexRangeSubject;
        public IObservable<RepeatedField<DeltaIndex>> ScoredDeltaIndexRange { get; }
        private Timer _timer;

        private readonly ConcurrentDictionary<int, DeltaIndexSyncItem> _deltaIndexSyncPool;
        public int MaxSyncPoolSize { get; } = 4;

        public bool IsPoolAvailable() { return MaxSyncPoolSize - _deltaIndexSyncPool.Count() > 0; }

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

            _scoredDeltaIndexRangeSubject =
                new ReplaySubject<RepeatedField<DeltaIndex>>(1, scheduler ?? Scheduler.Default);
            ScoredDeltaIndexRange = _scoredDeltaIndexRangeSubject.AsObservable();

            _deltaIndexSyncPool = new ConcurrentDictionary<int, DeltaIndexSyncItem>();
        }

        public bool PeersAvailable()
        {
            var arePeersAvailable = _peerRepository.GetActivePeers(PeerCount).Any();
            return arePeersAvailable;
        }

        public bool ContainsPeerHistory() { return _peerRepository.GetAll().Any(); }

        public async Task WaitForPeersAsync(CancellationToken cancellationToken = default)
        {
            while (!PeersAvailable())
            {
                await Task.Delay(1000, cancellationToken);
            }
        }

        public void GetDeltaIndexRangeFromPeers(int index, int range)
        {
            var deltaHistoryRequest = new DeltaHistoryRequest
                {Height = (uint) index, Range = (uint) range};

            var deltaIndexRange = new DeltaIndexSyncItem
            {
                Request = deltaHistoryRequest, DeltaIndexRangeRanked = new List<DeltaIndexScore>()
            };

            _deltaIndexSyncPool.TryAdd(index, deltaIndexRange);

            SendMessageToRandomPeers(deltaHistoryRequest);
        }

        public void GetDeltaHeight() { SendMessageToRandomPeers(new LatestDeltaHashRequest()); }

        private void SendMessageToRandomPeers(IMessage message)
        {
            var protocolMessage = message.ToProtocolMessage(_peerSettings.PeerId);
            var peers = _peerRepository.GetActivePeers(PeerCount);
            foreach (var peer in peers)
            {
                _peerClient.SendMessage(new MessageDto(
                    protocolMessage,
                    peer.PeerId));
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _peerService.MessageStream.Where(x => x.Payload != null &&
                    x.Payload.TypeUrl.EndsWith(typeof(DeltaHistoryResponse).ShortenedProtoFullName()))
               .Select(x => x.Payload.FromProtocolMessage<DeltaHistoryResponse>()).Subscribe(DeltaHistoryOnNext);

            OnCheckSyncPool();
            _timer = new Timer(OnCheckSyncPool, this, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public async Task StopAsync(CancellationToken cancellationToken) { }

        private void OnCheckSyncPool(object state) { ((PeerSyncManager) state).OnCheckSyncPool(); }

        private void OnCheckSyncPool()
        {
            if (_deltaIndexSyncPool.Keys.Count <= 0)
            {
                return;
            }

            foreach (var key in _deltaIndexSyncPool.Keys)
            {
                var deltaIndexSyncItem = _deltaIndexSyncPool[key];
                var combinedScores = deltaIndexSyncItem.DeltaIndexRangeRanked.Select(x => x.Score).Sum();

                if (combinedScores < PeerCount * 0.8)
                {
                    continue;
                }

                var deltaIndexScoredByDescendingOrder =
                    deltaIndexSyncItem.DeltaIndexRangeRanked.OrderByDescending(x => x.Score);
                var highestScoreDeltaIndexRange = deltaIndexScoredByDescendingOrder.First();
                var accuracyPercentage = highestScoreDeltaIndexRange.Score / (double) combinedScores * 100d;
                if (!(accuracyPercentage >= 99))
                {
                    continue;
                }

                if (_deltaIndexSyncPool.TryRemove(key, out _))
                {
                    _scoredDeltaIndexRangeSubject.OnNext(highestScoreDeltaIndexRange.DeltaIndexes);
                }
            }
        }

        private void DeltaHistoryOnNext(DeltaHistoryResponse deltaHistoryResponse)
        {
            if (deltaHistoryResponse.Result.Count == 0)
            {
                return;
            }

            var startHeight = deltaHistoryResponse.Result.First().Height;
            var newDeltaIndexScores = new DeltaIndexScore {DeltaIndexes = deltaHistoryResponse.Result, Score = 1};
            if (!_deltaIndexSyncPool.TryGetValue((int) startHeight, out var deltaIndexScores))
            {
                return;
            }

            var exists =
                deltaIndexScores.DeltaIndexRangeRanked.FirstOrDefault(x =>
                    x.DeltaIndexes.Equals(deltaHistoryResponse.Result));
            if (exists != null)
            {
                exists.Score++;
            }
            else
            {
                deltaIndexScores.DeltaIndexRangeRanked.Add(newDeltaIndexScores);
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
            _deltaHistorySubscription.Dispose();
        }
    }
}