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

using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Modules.Sync.Modal;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Catalyst.Core.Modules.Sync
{
    public class DeltaHeightRanker : PeerMessageRankManager<PeerId, LatestDeltaHashResponse>, IDeltaHeightRanker
    {
        private bool _disposed;
        private readonly double _threshold;
        private readonly IPeerRepository _peerRepository;
        private readonly int _maxPeersInStore;

        private readonly ReplaySubject<DeltaIndex> _foundDeltaHeightSubject;
        public IObservable<DeltaIndex> FoundDeltaHeight { get; }

        public DeltaHeightRanker(IPeerRepository peerRepository, int maxPeersInStore = 100, double threshold = 0.5, IScheduler scheduler = null)
        {
            _peerRepository = peerRepository;
            _maxPeersInStore = maxPeersInStore;
            _threshold = threshold;

            _foundDeltaHeightSubject = new ReplaySubject<DeltaIndex>(1, scheduler ?? Scheduler.Default);
            FoundDeltaHeight = _foundDeltaHeightSubject.AsObservable();
        }

        private int GetAvaliablePeerCount() => Math.Min(_maxPeersInStore, _peerRepository.Count());
        public IEnumerable<PeerId> GetPeers() => _messages.Keys;

        public override void Add(PeerId key, LatestDeltaHashResponse value)
        {
            base.Add(key, value);

            var mostPopularMessages = GetMessagesByMostPopular();
            ClearPeersOutOfRange(mostPopularMessages);

            var minimumScore = GetAvaliablePeerCount() * _threshold;
            if (mostPopularMessages.Where(x => x.Item.IsSync).Count() >= minimumScore || mostPopularMessages.Count() >= _peerRepository.Count())
            {
                _foundDeltaHeightSubject.OnNext(mostPopularMessages.First().Item.DeltaIndex);
            }
        }

        private void ClearPeersOutOfRange(IOrderedEnumerable<IRankedItem<LatestDeltaHashResponse>> mostPopularMessages)
        {
            while (_maxPeersInStore < _messages.Count())
            {
                var lastGroup = _messages.Where(kvp => kvp.Value == mostPopularMessages.Last().Item);
                var lastItem = lastGroup.Last();
                _messages.Remove(lastItem);
            }
        }

        public int Count()
        {
            return _messages.Count();
        }

        public IOrderedEnumerable<IRankedItem<LatestDeltaHashResponse>> GetMessagesByMostPopular(Func<KeyValuePair<PeerId, LatestDeltaHashResponse>, bool> filter = null)
        {
            return _messages.GroupBy(x => x.Value).Select(x => new RankedItem<LatestDeltaHashResponse> { Item = x.Key, Score = x.Count() }).OrderByDescending(x => x.Score).ThenByDescending(x => x.Item.DeltaIndex.Height);
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
                    _foundDeltaHeightSubject.Dispose();
                }
            }
            _disposed = true;
        }
    }
}
