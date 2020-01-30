using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Sync.Interface;
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
        private readonly IDisposable _deltaHistorySubscription;
        private readonly IUserOutput _userOutput;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly ReplaySubject<RepeatedField<DeltaIndex>> _scoredDeltaIndexRangeSubject;
        public IObservable<RepeatedField<DeltaIndex>> ScoredDeltaIndexRange { get; }

        private readonly ConcurrentDictionary<int, List<DeltaIndexScore>> _deltaIndexesScored;

        public PeerSyncManager(IPeerSettings peerSettings,
            IPeerClient peerClient,
            IPeerRepository peerRepository,
            IPeerClientObservable deltaHistoryResponseObserver,
            IUserOutput userOutput,
            IDeltaIndexService deltaIndexService,
            IScheduler scheduler = null)
        {
            _peerSettings = peerSettings;
            _peerClient = peerClient;
            _peerRepository = peerRepository;
            _deltaHistorySubscription = deltaHistoryResponseObserver.MessageStream.Subscribe(DeltaHistoryOnNext);
            _userOutput = userOutput;
            _deltaIndexService = deltaIndexService;

            _scoredDeltaIndexRangeSubject = new ReplaySubject<RepeatedField<DeltaIndex>>(1, scheduler ?? Scheduler.Default);
            ScoredDeltaIndexRange = _scoredDeltaIndexRangeSubject.AsObservable();

            _deltaIndexesScored = new ConcurrentDictionary<int, List<DeltaIndexScore>>();
        }

        public void GetDeltaIndexRangeFromPeers(IMessage message) { SendMessageToRandomPeers(message); }

        public void GetDeltaHeight()
        {
            SendMessageToRandomPeers(new LatestDeltaHashRequest());
        }

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

        public async Task StartAsync()
        {
            await Task.Run(WorkerAsync);
        }

        private async Task WorkerAsync()
        {
            while (true)
            {
                //await ProgressAsync();
                var key = _deltaIndexesScored.Keys.FirstOrDefault();
                {
                    if (_deltaIndexService.Height() > key)
                    {
                        continue;
                    }

                    var deltaIndexScored = _deltaIndexesScored[key];
                    var combinedScores = deltaIndexScored.Select(x => x.Score).Aggregate((x, y) => x + y);

                    if (combinedScores >= PeerCount)
                    {
                        var deltaIndexScoredByDescendingOrder = deltaIndexScored.OrderByDescending(x => x.Score);
                        var highestScoreDeltaIndexRange = deltaIndexScoredByDescendingOrder.First();
                        var accuracyPercentage = highestScoreDeltaIndexRange.Score / (double) combinedScores * 100d;
                        if (accuracyPercentage >= 99)
                        {
                            _deltaIndexesScored.TryRemove(key, out _);
                            _scoredDeltaIndexRangeSubject.OnNext(highestScoreDeltaIndexRange.DeltaIndexes);
                        }
                    }
                }

                await Task.Delay(100);
            }
        }

        private void DeltaHistoryOnNext(IPeerClientMessageDto peerClientMessageDto)
        {
            if (!(peerClientMessageDto.Message is DeltaHistoryResponse deltaHistoryResponse))
            {
                return;
            }

            var startHeight = deltaHistoryResponse.Result.First().Height;

            var newDeltaIndexScores = new DeltaIndexScore {DeltaIndexes = deltaHistoryResponse.Result, Score = 1};
            if (_deltaIndexesScored.TryGetValue((int) startHeight, out var deltaIndexScores))
            {
                var exists = deltaIndexScores.FirstOrDefault(x => x.DeltaIndexes.Equals(deltaHistoryResponse.Result));
                if (exists != null)
                {
                    exists.Score++;
                }
                else
                {
                    deltaIndexScores.Add(newDeltaIndexScores);
                }
            }
            else
            {
                _deltaIndexesScored.TryAdd((int) startHeight,
                    new List<DeltaIndexScore> {newDeltaIndexScores});
            }
        }

        public void Dispose() { _deltaHistorySubscription.Dispose(); }
    }
}
